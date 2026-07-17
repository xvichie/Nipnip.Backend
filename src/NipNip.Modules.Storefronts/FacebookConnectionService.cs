using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Crypto;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class FacebookConnectionService(
    AppDbContext db,
    StoreService storeService,
    ProductService productService,
    GraphApiClient graph,
    AesStringProtector protector,
    IOptions<FacebookOptions> options)
{
    private const int StateValidMinutes = 10;
    private const string Scopes = "pages_show_list,pages_read_engagement,pages_manage_posts";
    private readonly FacebookOptions _options = options.Value;

    private record StatePayload(Guid StoreId, long IssuedAtUnix);
    private record PendingPayload(Guid StoreId, long IssuedAtUnix, List<FacebookPageAccount> Pages);

    public async Task<string> BuildConnectUrlAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var state = Protect(new StatePayload(store.Id, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        // Facebook Login for Business rejects a free-form `scope` list ("Invalid Scopes")
        // — permissions must be pre-declared in a saved Login Connection and referenced by
        // config_id instead. Classic Facebook Login still takes `scope` directly.
        var permissionParam = string.IsNullOrWhiteSpace(_options.ConfigId)
            ? $"scope={Uri.EscapeDataString(Scopes)}"
            : $"config_id={Uri.EscapeDataString(_options.ConfigId)}";

        // auth_type=rerequest forces Facebook to re-prompt for permissions the person
        // declined/skipped on a previous login — without it, a plain reconnect silently
        // reuses the old grant and any missing permission (e.g. pages_read_engagement)
        // never gets asked for again.
        return "https://www.facebook.com/v21.0/dialog/oauth" +
               $"?client_id={Uri.EscapeDataString(_options.AppId)}" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               $"&{permissionParam}" +
               $"&auth_type=rerequest" +
               $"&response_type=code" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    /// <summary>
    /// Always returns a frontend URL to redirect the browser to (success, error, or a
    /// "choose a Page" step) — never throws. This runs on a plain browser navigation
    /// back from facebook.com, so there's no good way to show a raw JSON error here.
    /// </summary>
    public async Task<string> HandleCallbackAsync(string? code, string? state)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
                throw new ArgumentException("Missing authorization code.");

            var payload = Unprotect<StatePayload>(state);
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - payload.IssuedAtUnix > StateValidMinutes * 60)
                throw new ArgumentException("This connection attempt expired. Please try again.");

            var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == payload.StoreId)
                ?? throw new NotFoundException("Store not found.");

            var shortLivedToken = await graph.ExchangeCodeAsync(code);
            var longLivedToken = await graph.ExchangeLongLivedTokenAsync(shortLivedToken);
            var pages = await graph.GetManagedPagesAsync(longLivedToken);

            if (pages.Count == 0)
                return $"{FrontendUrl}/dashboard/merchant/store/integrations?fb=error&reason=no_pages";

            if (pages.Count == 1)
            {
                await ConnectPageAsync(store.Id, pages[0]);
                return $"{FrontendUrl}/dashboard/merchant/store/integrations?fb=connected";
            }

            var pending = Protect(new PendingPayload(store.Id, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), pages));
            return $"{FrontendUrl}/dashboard/merchant/store/integrations?fb=choose&pending={Uri.EscapeDataString(pending)}";
        }
        catch (Exception)
        {
            return $"{FrontendUrl}/dashboard/merchant/store/integrations?fb=error";
        }
    }

    public async Task<List<FacebookPendingPageResponse>> GetPendingChoicesAsync(string clerkUserId, string pendingToken)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var pending = ResolvePending(pendingToken, store.Id);
        return pending.Pages.Select(p => new FacebookPendingPageResponse(p.Id, p.Name)).ToList();
    }

    public async Task SelectPageAsync(string clerkUserId, SelectFacebookPageRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var pending = ResolvePending(request.Pending, store.Id);

        var page = pending.Pages.FirstOrDefault(p => p.Id == request.PageId)
            ?? throw new ArgumentException("That Page wasn't part of this connection attempt.");

        await ConnectPageAsync(store.Id, page);
    }

    public async Task<FacebookStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new FacebookStatusResponse(store.FacebookConnectedAt.HasValue, store.FacebookPageName);
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.FacebookPageId = null;
        store.FacebookPageName = null;
        store.FacebookPageAccessTokenEncrypted = null;
        store.FacebookConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<List<FacebookPostSummaryResponse>> GetPostsAsync(string clerkUserId)
    {
        var (pageId, pageToken) = await GetConnectedPageAsync(clerkUserId);
        var posts = await graph.GetPagePostsAsync(pageId, pageToken);
        return posts.Select(p => new FacebookPostSummaryResponse(p.Id, p.Message, p.CreatedTime, p.ThumbnailUrl, p.HasVideo)).ToList();
    }

    public async Task<FacebookPostDetailResponse> GetPostDetailAsync(string clerkUserId, string postId)
    {
        var (_, pageToken) = await GetConnectedPageAsync(clerkUserId);
        var detail = await graph.GetPostDetailAsync(postId, pageToken);

        string? videoUrl = null;
        if (detail.VideoNodeId is not null)
            videoUrl = await graph.GetVideoSourceAsync(detail.VideoNodeId, pageToken);

        return new FacebookPostDetailResponse(detail.Message, detail.ImageUrls, videoUrl);
    }

    public async Task<FacebookProductPreviewResponse> PreviewProductPostAsync(string clerkUserId, Guid productId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);
        var coverImageUrl = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;

        return new FacebookProductPreviewResponse(ProductPostMessageBuilder.Build(product, store), coverImageUrl);
    }

    public async Task<string> PublishProductAsync(string clerkUserId, Guid productId, string? message)
    {
        var (pageId, pageToken) = await GetConnectedPageAsync(clerkUserId);
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var finalMessage = string.IsNullOrWhiteSpace(message) ? ProductPostMessageBuilder.Build(product, store) : message.Trim();
        var coverImageUrl = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;

        var postId = await graph.PublishPostAsync(pageId, pageToken, finalMessage, coverImageUrl);
        return $"https://www.facebook.com/{postId}";
    }

    private async Task<(string PageId, string PageToken)> GetConnectedPageAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        if (store.FacebookPageId is null || store.FacebookPageAccessTokenEncrypted is null)
            throw new ConflictException("Connect your Facebook Page first.");

        return (store.FacebookPageId, protector.Decrypt(store.FacebookPageAccessTokenEncrypted));
    }

    private async Task ConnectPageAsync(Guid storeId, FacebookPageAccount page)
    {
        var store = await db.Stores.FirstAsync(s => s.Id == storeId);
        store.FacebookPageId = page.Id;
        store.FacebookPageName = page.Name;
        store.FacebookPageAccessTokenEncrypted = protector.Encrypt(page.AccessToken);
        store.FacebookConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private PendingPayload ResolvePending(string pendingToken, Guid callerStoreId)
    {
        var pending = Unprotect<PendingPayload>(pendingToken);
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - pending.IssuedAtUnix > StateValidMinutes * 60)
            throw new ArgumentException("This connection attempt expired. Please try again.");
        if (pending.StoreId != callerStoreId)
            throw new ForbiddenException("This connection attempt belongs to a different store.");

        return pending;
    }

    private string Protect<T>(T payload) => protector.Encrypt(JsonSerializer.Serialize(payload));

    private T Unprotect<T>(string token)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(protector.Decrypt(token))
                ?? throw new ArgumentException("Invalid or expired request.");
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException("Invalid or expired request.");
        }
    }

    private string FrontendUrl => _options.FrontendUrl.TrimEnd('/');
}
