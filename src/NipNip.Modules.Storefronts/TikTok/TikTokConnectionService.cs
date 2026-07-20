using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Crypto;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.TikTok;

public class TikTokConnectionService(
    AppDbContext db,
    StoreService storeService,
    ProductService productService,
    TikTokApiClient tiktok,
    AesStringProtector protector,
    IOptions<TikTokOptions> options,
    ILogger<TikTokConnectionService> logger)
{
    private const int StateValidMinutes = 10;
    private const int MaxPublishImages = 10;
    private const int TokenExpirySafetyBufferSeconds = 60;
    private readonly TikTokOptions _options = options.Value;

    private record StatePayload(Guid StoreId, long IssuedAtUnix);

    public async Task<string> BuildConnectUrlAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var state = Protect(new StatePayload(store.Id, DateTimeOffset.UtcNow.ToUnixTimeSeconds()));

        // user.info.basic identifies the account for the "Connected as ..." status line;
        // video.publish is what lets us call Direct Post for the export flow.
        const string scopes = "user.info.basic,video.publish";

        return "https://www.tiktok.com/v2/auth/authorize/" +
               $"?client_key={Uri.EscapeDataString(_options.ClientKey)}" +
               $"&scope={Uri.EscapeDataString(scopes)}" +
               "&response_type=code" +
               $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
               $"&state={Uri.EscapeDataString(state)}";
    }

    // Always returns a frontend URL to redirect the browser to — same reasoning as
    // FacebookConnectionService.HandleCallbackAsync: this runs on a plain browser
    // navigation back from tiktok.com, so there's no good way to show a raw JSON error here.
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

            var token = await tiktok.ExchangeCodeAsync(code);
            logger.LogInformation("TikTok OAuth granted scope: {Scope}", token.Scope);
            var userInfo = await tiktok.GetUserInfoAsync(token.AccessToken);

            store.TikTokOpenId = token.OpenId;
            store.TikTokDisplayName = userInfo.DisplayName;
            store.TikTokAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
            store.TikTokRefreshTokenEncrypted = protector.Encrypt(token.RefreshToken);
            store.TikTokTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
            store.TikTokConnectedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();

            return $"{FrontendUrl}/dashboard/merchant/store/integrations?tiktok=connected";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TikTok OAuth callback failed");
            return $"{FrontendUrl}/dashboard/merchant/store/integrations?tiktok=error";
        }
    }

    public async Task<TikTokStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new TikTokStatusResponse(store.TikTokConnectedAt.HasValue, store.TikTokDisplayName);
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.TikTokOpenId = null;
        store.TikTokDisplayName = null;
        store.TikTokAccessTokenEncrypted = null;
        store.TikTokRefreshTokenEncrypted = null;
        store.TikTokTokenExpiresAt = null;
        store.TikTokConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<TikTokProductPreviewResponse> PreviewProductPostAsync(string clerkUserId, Guid productId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var imageUrls = product.Images.OrderBy(i => i.SortOrder).Take(MaxPublishImages).Select(i => i.Url).ToList();
        var title = product.Name.Length > 90 ? product.Name[..90] : product.Name;

        return new TikTokProductPreviewResponse(imageUrls, title, ProductPostMessageBuilder.Build(product, store));
    }

    public async Task<TikTokPublishResponse> PublishProductAsync(string clerkUserId, Guid productId, TikTokPublishRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);
        var imageUrls = product.Images.OrderBy(i => i.SortOrder).Take(MaxPublishImages).Select(i => i.Url).ToList();

        // Both fall back to the same defaults the preview endpoint shows, so the "automatic
        // sharing on create" flow (which has no preview step to seed a title/description from)
        // still gets a sensible post instead of requiring the merchant to type one first.
        var title = string.IsNullOrWhiteSpace(request.Title)
            ? (product.Name.Length > 90 ? product.Name[..90] : product.Name)
            : request.Title.Trim();
        var description = string.IsNullOrWhiteSpace(request.Description)
            ? ProductPostMessageBuilder.Build(product, store)
            : request.Description.Trim();
        if (!string.IsNullOrWhiteSpace(request.Hashtags))
            description = $"{description}\n\n{request.Hashtags.Trim()}";

        return await PublishAsync(store, imageUrls, title, description, request.AutoAddMusic);
    }

    // Ad-hoc publish path used by the Social Post Creator tool — the source image isn't tied
    // to a Product row (it's a freshly-generated canvas composite uploaded straight to
    // Cloudinary), so this takes image URLs directly instead of looking them up from a product.
    // The caller controls both which images to include and their order (used as the photo
    // carousel order, with the first image also becoming the cover).
    public async Task<TikTokPublishResponse> PublishImagesAsync(string clerkUserId, TikTokPublishImagesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var imageUrls = request.ImageUrls.Take(MaxPublishImages).ToList();

        return await PublishAsync(store, imageUrls, request.Title, request.Description, request.AutoAddMusic);
    }

    private async Task<TikTokPublishResponse> PublishAsync(Store store, List<string> imageUrls, string title, string description, bool autoAddMusic)
    {
        if (store.TikTokAccessTokenEncrypted is null)
            throw new ConflictException("Connect your TikTok account first.");
        if (imageUrls.Count == 0)
            throw new ArgumentException("TikTok photo posts need at least one photo.");

        // TikTok's photo post endpoint only accepts PULL_FROM_URL against a domain WE own and
        // have verified with TikTok — our images live on Cloudinary's shared domain, so each
        // one is rewritten to route through our own frontend's proxy first.
        var proxiedUrls = imageUrls.Select(BuildProxiedImageUrl).ToList();

        var accessToken = await GetAccessTokenAsync(store);
        var creatorInfo = await tiktok.QueryCreatorInfoAsync(accessToken);
        logger.LogInformation("TikTok creator_info privacy_level_options: {Options}", string.Join(",", creatorInfo.PrivacyLevelOptions));

        // Unaudited apps are restricted to posting privately regardless of the connected
        // account's own privacy setting — always prefer SELF_ONLY when TikTok offers it rather
        // than trusting list order, since the first option returned isn't guaranteed to be it.
        var privacyLevel = creatorInfo.PrivacyLevelOptions.Contains("SELF_ONLY")
            ? "SELF_ONLY"
            : creatorInfo.PrivacyLevelOptions.FirstOrDefault()
                ?? throw new ArgumentException("TikTok didn't return any privacy level options for this account.");

        var publishId = await tiktok.InitPhotoPostAsync(accessToken, title.Trim(), description.Trim(), privacyLevel, proxiedUrls, 0, autoAddMusic);

        // Best-effort short poll — TikTok processes the post asynchronously, so a failure
        // past this window (or a still-PROCESSING result) doesn't mean anything went wrong,
        // just that this call doesn't wait for it.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await Task.Delay(1500);
            var status = await tiktok.GetPublishStatusAsync(accessToken, publishId);
            logger.LogInformation("TikTok publish {PublishId} status: {Status} (fail_reason: {FailReason})", publishId, status.Status, status.FailReason);
            if (status.Status == "FAILED")
                throw new ArgumentException($"TikTok rejected this post ({status.FailReason ?? "no reason given"}) — double-check your images and try again.");
            if (status.Status == "PUBLISH_COMPLETE") break;
        }

        return new TikTokPublishResponse(true, privacyLevel);
    }

    private string BuildProxiedImageUrl(string cloudinaryUrl)
    {
        // TikTok only accepts JPEG/WebP for photo posts, so the proxy always transcodes to JPEG
        // regardless of the source format — the proxy URL's own extension should match that,
        // since TikTok's format check appears to key off the fetched URL rather than only the
        // Content-Type header.
        return $"{FrontendUrl}/api/media/tiktok-proxy/photo.jpg?src={Uri.EscapeDataString(cloudinaryUrl)}";
    }

    private async Task<string> GetAccessTokenAsync(Store store)
    {
        if (store.TikTokAccessTokenEncrypted is null || store.TikTokRefreshTokenEncrypted is null)
            throw new ConflictException("Connect your TikTok account first.");

        if (store.TikTokTokenExpiresAt.HasValue
            && store.TikTokTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddSeconds(TokenExpirySafetyBufferSeconds))
        {
            return protector.Decrypt(store.TikTokAccessTokenEncrypted);
        }

        var refreshToken = protector.Decrypt(store.TikTokRefreshTokenEncrypted);
        var token = await tiktok.RefreshTokenAsync(refreshToken);

        // TikTok rotates the refresh token on every use — the old one stops working, so both
        // must be re-saved together or the next refresh attempt would fail.
        store.TikTokAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
        store.TikTokRefreshTokenEncrypted = protector.Encrypt(token.RefreshToken);
        store.TikTokTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
        await db.SaveChangesAsync();

        return token.AccessToken;
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
