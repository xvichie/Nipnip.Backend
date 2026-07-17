using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Crypto;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

// Rides on the same Facebook Page connection/token as FacebookConnectionService — an
// Instagram Business/Creator account is discovered via the linked Page, not a separate
// OAuth flow, so there's nothing here to "connect" beyond having a Facebook Page linked.
public class InstagramConnectionService(
    StoreService storeService,
    ProductService productService,
    GraphApiClient graph,
    AesStringProtector protector)
{
    public async Task<InstagramStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var connection = await TryGetConnectedAccountAsync(clerkUserId);
        return new InstagramStatusResponse(connection is not null, connection?.Account.Username);
    }

    public async Task<List<InstagramMediaSummaryResponse>> GetMediaAsync(string clerkUserId)
    {
        var (account, pageToken) = await GetConnectedAccountAsync(clerkUserId);
        var media = await graph.GetInstagramMediaAsync(account.Id, pageToken);
        return media.Select(m => new InstagramMediaSummaryResponse(m.Id, m.Caption, m.ThumbnailUrl, m.HasVideo)).ToList();
    }

    public async Task<InstagramMediaDetailResponse> GetMediaDetailAsync(string clerkUserId, string mediaId)
    {
        var (_, pageToken) = await GetConnectedAccountAsync(clerkUserId);
        var detail = await graph.GetInstagramMediaDetailAsync(mediaId, pageToken);
        return new InstagramMediaDetailResponse(detail.Caption, detail.ImageUrls, detail.VideoUrl);
    }

    public async Task<InstagramProductPreviewResponse> PreviewProductPostAsync(string clerkUserId, Guid productId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);
        var coverImageUrl = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url;

        return new InstagramProductPreviewResponse(ProductPostMessageBuilder.Build(product, store), coverImageUrl);
    }

    public async Task<string> PublishProductAsync(string clerkUserId, Guid productId, string? message)
    {
        var (account, pageToken) = await GetConnectedAccountAsync(clerkUserId);
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var coverImageUrl = product.Images.OrderBy(i => i.SortOrder).FirstOrDefault()?.Url
            ?? throw new ArgumentException("Instagram posts need at least one photo — add one to this product first.");

        var finalMessage = string.IsNullOrWhiteSpace(message) ? ProductPostMessageBuilder.Build(product, store) : message.Trim();

        return await graph.PublishInstagramPostAsync(account.Id, pageToken, finalMessage, coverImageUrl, account.Username);
    }

    private async Task<(InstagramAccount Account, string PageToken)> GetConnectedAccountAsync(string clerkUserId)
    {
        var connection = await TryGetConnectedAccountAsync(clerkUserId);
        if (connection is null)
            throw new ConflictException("Connect a Facebook Page with a linked Instagram Business account first.");
        return connection.Value;
    }

    private async Task<(InstagramAccount Account, string PageToken)?> TryGetConnectedAccountAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        if (store.FacebookPageId is null || store.FacebookPageAccessTokenEncrypted is null)
            return null;

        var pageToken = protector.Decrypt(store.FacebookPageAccessTokenEncrypted);
        var account = await graph.GetInstagramBusinessAccountAsync(store.FacebookPageId, pageToken);
        return account is null ? null : (account, pageToken);
    }
}
