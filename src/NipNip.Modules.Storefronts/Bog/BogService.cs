using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Modules.Tracking;
using NipNip.Shared.Crypto;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;
using NipNip.Modules.Storefronts;

namespace NipNip.Modules.Storefronts.Bog;

public class BogService(
    AppDbContext db,
    StoreService storeService,
    BogApiClient bogApi,
    AesStringProtector protector,
    IEmailService emailService,
    TrackingService trackingService,
    StoreDiscountCodeService discountCodeService,
    IOptions<BogOptions> options,
    ILogger<BogService> logger)
{
    private readonly BogOptions _options = options.Value;
    private const int TokenExpirySafetyBufferSeconds = 60;

    // --- Merchant-side connection (credentials entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectBogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new ArgumentException("Client ID is required.");
        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new ArgumentException("Client secret is required.");

        // Validate immediately by requesting a token, so a typo surfaces at connect-time
        // rather than at the first real checkout (mirrors QuickShipperService/TbcService).
        await bogApi.RequestTokenAsync(request.ClientId.Trim(), request.ClientSecret);

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.BogClientId = request.ClientId.Trim();
        store.BogClientSecretEncrypted = protector.Encrypt(request.ClientSecret);
        store.BogAccessTokenEncrypted = null;
        store.BogTokenExpiresAt = null;
        store.BogConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.BogClientId = null;
        store.BogClientSecretEncrypted = null;
        store.BogAccessTokenEncrypted = null;
        store.BogTokenExpiresAt = null;
        store.BogConnectedAt = null;

        // Defensive: a store shouldn't keep advertising BOG as a checkout option once its
        // credentials are gone — otherwise a real customer could pick "pay by card" and hit
        // a hard error mid-checkout.
        DisableBogInThemeConfig(store);

        await db.SaveChangesAsync();
    }

    public async Task<BogStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new BogStatusResponse(store.BogConnectedAt.HasValue, store.BogClientId, store.BogConnectedAt);
    }

    private static void DisableBogInThemeConfig(Store store)
    {
        try
        {
            if (JsonNode.Parse(store.ThemeConfig) is not JsonObject config) return;
            if (config["bogEnabled"]?.GetValue<bool>() != true) return;
            config["bogEnabled"] = false;
            store.ThemeConfig = config.ToJsonString();
        }
        catch (JsonException)
        {
            // Malformed theme config isn't this method's concern to fix.
        }
    }

    private async Task<string> GetAccessTokenAsync(Store store)
    {
        if (store.BogClientId is null || store.BogClientSecretEncrypted is null)
            throw new ConflictException("Connect your Bank of Georgia account first.");

        if (store.BogAccessTokenEncrypted is not null
            && store.BogTokenExpiresAt.HasValue
            && store.BogTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddSeconds(TokenExpirySafetyBufferSeconds))
        {
            return protector.Decrypt(store.BogAccessTokenEncrypted);
        }

        var clientSecret = protector.Decrypt(store.BogClientSecretEncrypted);
        var token = await bogApi.RequestTokenAsync(store.BogClientId, clientSecret);

        store.BogAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
        store.BogTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
        await db.SaveChangesAsync();

        return token.AccessToken;
    }

    // --- Checkout session creation (called from CartService.CheckoutAsync) ---

    public async Task<string> CreateCheckoutSessionAsync(Order order, Store store, string merchantData)
    {
        if (store.BogClientId is null || store.BogClientSecretEncrypted is null)
            throw new ConflictException("This store hasn't set up Bank of Georgia payments yet.");

        var accessToken = await GetAccessTokenAsync(store);
        var callbackUrl = $"{_options.ApiBaseUrl.TrimEnd('/')}/api/webhooks/bog";
        var description = $"Order at {store.Name}";

        var requestBody = new
        {
            callback_url = callbackUrl,
            description,
            external_order_id = order.Id.ToString("N"),
            purchase_units = new { currency = "GEL", total_amount = order.Total },
            ttl = 1440,
        };

        var result = await bogApi.CreatePreOrderAsync(accessToken, requestBody);

        order.BogPreOrderId = result.PreOrderId;
        order.BogMerchantData = merchantData;
        await db.SaveChangesAsync();

        return result.PaymentLink;
    }

    // --- Callback handling ---
    //
    // BOG's callback body is self-contained and signed (Callback-Signature header, SHA256withRSA
    // over the raw body), but verifying it requires BOG's public key, which isn't available to
    // us. Rather than trust an unverified body, this only reads the pre_order_id out of it as a
    // "check now" trigger and re-pulls the real status via our own authenticated GET request —
    // the same notify-then-pull shape as TbcService, chosen for the same reason (no way to
    // cryptographically trust the callback payload itself).

    private record BogMerchantData(string? SessionId, string? Ref);

    public async Task HandleCallbackAsync(string preOrderId)
    {
        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .Include(o => o.BundleItems).ThenInclude(i => i.Bundle)
            .FirstOrDefaultAsync(o => o.BogPreOrderId == preOrderId);
        if (order is null)
        {
            logger.LogWarning("BOG callback for unknown pre-order {PreOrderId}.", preOrderId);
            return;
        }

        var store = order.Store;
        string statusKey;
        try
        {
            var accessToken = await GetAccessTokenAsync(store);
            var result = await bogApi.GetPreOrderStatusAsync(accessToken, preOrderId);
            statusKey = result.StatusKey;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch BOG pre-order status for {PreOrderId}.", preOrderId);
            return;
        }

        var wasAlreadyConfirmed = order.Status == OrderStatus.Confirmed;

        switch (statusKey)
        {
            case "performed":
                order.Status = OrderStatus.Confirmed;
                order.PaymentConfirmedAt ??= DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                // BOG may deliver the callback more than once — this guard keeps a duplicate
                // "performed" delivery from double-sending the email or double-counting the
                // affiliate conversion.
                if (!wasAlreadyConfirmed)
                    await FinalizeApprovedOrderAsync(order, store);
                break;

            case "deactivated":
            case "expired":
                if (order.Status != OrderStatus.Cancelled)
                {
                    order.Status = OrderStatus.Cancelled;
                    OrderStockAdjuster.Release(order);
                }
                await db.SaveChangesAsync();
                break;

            default:
                // "active" — leave Pending, a later callback will follow.
                break;
        }
    }

    private async Task FinalizeApprovedOrderAsync(Order order, Store store)
    {
        var merchantData = ParseMerchantData(order.BogMerchantData);

        if (merchantData.SessionId is not null)
        {
            var cart = await db.Carts.Include(c => c.Items).Include(c => c.BundleItems)
                .FirstOrDefaultAsync(c => c.StoreId == store.Id && c.SessionId == merchantData.SessionId);
            if (cart is not null && (cart.Items.Count > 0 || cart.BundleItems.Count > 0))
            {
                db.CartItems.RemoveRange(cart.Items);
                db.CartBundleItems.RemoveRange(cart.BundleItems);
                await db.SaveChangesAsync();
            }
        }

        if (order.DiscountCode is not null)
        {
            if (!await discountCodeService.ConfirmUsageAsync(store.Id, order.DiscountCode))
                logger.LogWarning("Discount code {Code} could not be confirmed for BOG order {OrderId} — it likely hit its usage limit concurrently.", order.DiscountCode, order.Id);
        }

        if (store.AffiliateEnabled)
        {
            try
            {
                await trackingService.TrackStorefrontConversionAsync(
                    store.MerchantId, merchantData.Ref, order.Id.ToString(), order.Total, "GEL");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record storefront affiliate conversion for BOG order {OrderId}", order.Id);
            }
        }

        try
        {
            var emailItems = order.Items
                .Select(i => new OrderConfirmationEmailItem(i.Variant.Product.DisplayName(), i.Quantity, i.PriceAtPurchase))
                .Concat(order.BundleItems.Select(i => new OrderConfirmationEmailItem(i.Bundle.Name, i.Quantity, i.PriceAtPurchase)))
                .ToList();

            await emailService.SendOrderConfirmationAsync(new OrderConfirmationEmailData(
                order.Email,
                order.CustomerName,
                store.Name,
                store.Slug,
                order.Id.ToString(),
                emailItems,
                order.Total,
                order.ShippingFee,
                order.ShippingZoneName,
                order.PaymentMethod.ToString(),
                null
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order confirmation email for BOG order {OrderId}", order.Id);
        }
    }

    private static BogMerchantData ParseMerchantData(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return new BogMerchantData(null, null);
        try
        {
            return JsonSerializer.Deserialize<BogMerchantData>(raw) ?? new BogMerchantData(null, null);
        }
        catch (JsonException)
        {
            return new BogMerchantData(null, null);
        }
    }
}
