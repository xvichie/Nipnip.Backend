using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Tracking;
using NipNip.Shared.Crypto;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.Tbc;

public class TbcService(
    AppDbContext db,
    StoreService storeService,
    TbcApiClient tbcApi,
    AesStringProtector protector,
    IEmailService emailService,
    TrackingService trackingService,
    IOptions<TbcOptions> options,
    ILogger<TbcService> logger)
{
    private readonly TbcOptions _options = options.Value;
    private const int TokenExpirySafetyBufferSeconds = 60;

    // --- Merchant-side connection (credentials entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectTbcRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new ArgumentException("Client ID is required.");
        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new ArgumentException("Client secret is required.");

        // Validate immediately by requesting a token, so a typo surfaces at connect-time
        // rather than at the first real checkout (mirrors QuickShipperService.ConnectAsync).
        await tbcApi.RequestTokenAsync(_options.ApiKey, request.ClientId.Trim(), request.ClientSecret);

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.TbcClientId = request.ClientId.Trim();
        store.TbcClientSecretEncrypted = protector.Encrypt(request.ClientSecret);
        store.TbcAccessTokenEncrypted = null;
        store.TbcTokenExpiresAt = null;
        store.TbcConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.TbcClientId = null;
        store.TbcClientSecretEncrypted = null;
        store.TbcAccessTokenEncrypted = null;
        store.TbcTokenExpiresAt = null;
        store.TbcConnectedAt = null;

        // Defensive: a store shouldn't keep advertising TBC as a checkout option once its
        // credentials are gone — otherwise a real customer could pick "pay by card" and hit
        // a hard error mid-checkout.
        DisableTbcInThemeConfig(store);

        await db.SaveChangesAsync();
    }

    public async Task<TbcStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new TbcStatusResponse(store.TbcConnectedAt.HasValue, store.TbcClientId, store.TbcConnectedAt);
    }

    private static void DisableTbcInThemeConfig(Store store)
    {
        try
        {
            if (JsonNode.Parse(store.ThemeConfig) is not JsonObject config) return;
            if (config["tbcEnabled"]?.GetValue<bool>() != true) return;
            config["tbcEnabled"] = false;
            store.ThemeConfig = config.ToJsonString();
        }
        catch (JsonException)
        {
            // Malformed theme config isn't this method's concern to fix.
        }
    }

    private async Task<string> GetAccessTokenAsync(Store store)
    {
        if (store.TbcClientId is null || store.TbcClientSecretEncrypted is null)
            throw new ConflictException("Connect your TBC account first.");

        if (store.TbcAccessTokenEncrypted is not null
            && store.TbcTokenExpiresAt.HasValue
            && store.TbcTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddSeconds(TokenExpirySafetyBufferSeconds))
        {
            return protector.Decrypt(store.TbcAccessTokenEncrypted);
        }

        var clientSecret = protector.Decrypt(store.TbcClientSecretEncrypted);
        var token = await tbcApi.RequestTokenAsync(_options.ApiKey, store.TbcClientId, clientSecret);

        store.TbcAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
        store.TbcTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
        await db.SaveChangesAsync();

        return token.AccessToken;
    }

    // --- Checkout session creation (called from CartService.CheckoutAsync) ---

    public async Task<string> CreateCheckoutSessionAsync(Order order, Store store, string merchantData)
    {
        if (store.TbcClientId is null || store.TbcClientSecretEncrypted is null)
            throw new ConflictException("This store hasn't set up TBC payments yet.");

        var accessToken = await GetAccessTokenAsync(store);
        var returnUrl = $"{_options.FrontendUrl.TrimEnd('/')}/store/{store.Slug}/checkout/confirmation?orderId={order.Id}";
        var callbackUrl = $"{_options.ApiBaseUrl.TrimEnd('/')}/api/webhooks/tbc";
        var description = $"Order at {store.Name}";
        if (description.Length > 30) description = description[..30];

        var requestBody = new
        {
            amount = new { currency = "GEL", total = order.Total },
            returnurl = returnUrl,
            callbackUrl,
            merchantPaymentId = order.Id.ToString("N"),
            description,
            language = "KA",
        };

        var result = await tbcApi.CreatePaymentAsync(_options.ApiKey, accessToken, requestBody);

        order.TbcPaymentId = result.PayId;
        order.TbcMerchantData = merchantData;
        await db.SaveChangesAsync();

        return result.ApprovalUrl;
    }

    // --- Callback handling (notify-then-pull: the callback body only carries a payment id, so
    // the real status must be fetched via GET /payments/{payId} — no signature to verify) ---

    private record TbcMerchantData(string? SessionId, string? Ref);

    public async Task HandleCallbackAsync(string paymentId)
    {
        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.TbcPaymentId == paymentId);
        if (order is null)
        {
            logger.LogWarning("TBC callback for unknown payment {PaymentId}.", paymentId);
            return;
        }

        var store = order.Store;
        string status;
        try
        {
            var accessToken = await GetAccessTokenAsync(store);
            var result = await tbcApi.GetPaymentStatusAsync(_options.ApiKey, accessToken, paymentId);
            status = result.Status;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch TBC payment status for {PaymentId}.", paymentId);
            return;
        }

        var wasAlreadyConfirmed = order.Status == OrderStatus.Confirmed;

        switch (status)
        {
            case "Succeeded":
                order.Status = OrderStatus.Confirmed;
                order.PaymentConfirmedAt ??= DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                // TBC may deliver the callback more than once — this guard keeps a duplicate
                // "Succeeded" delivery from double-sending the email or double-counting the
                // affiliate conversion.
                if (!wasAlreadyConfirmed)
                    await FinalizeApprovedOrderAsync(order, store);
                break;

            case "Failed":
            case "Expired":
                order.Status = OrderStatus.Cancelled;
                await db.SaveChangesAsync();
                break;

            default:
                // Created/Processing/WaitingConfirm/etc — leave Pending, a later callback will follow.
                break;
        }
    }

    private async Task FinalizeApprovedOrderAsync(Order order, Store store)
    {
        var merchantData = ParseMerchantData(order.TbcMerchantData);

        if (merchantData.SessionId is not null)
        {
            var cart = await db.Carts.Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.StoreId == store.Id && c.SessionId == merchantData.SessionId);
            if (cart is not null && cart.Items.Count > 0)
            {
                db.CartItems.RemoveRange(cart.Items);
                await db.SaveChangesAsync();
            }
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
                logger.LogError(ex, "Failed to record storefront affiliate conversion for TBC order {OrderId}", order.Id);
            }
        }

        try
        {
            var emailItems = order.Items
                .Select(i => new OrderConfirmationEmailItem(i.Variant.Product.Name, i.Quantity, i.PriceAtPurchase))
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
            logger.LogError(ex, "Failed to send order confirmation email for TBC order {OrderId}", order.Id);
        }
    }

    private static TbcMerchantData ParseMerchantData(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return new TbcMerchantData(null, null);
        try
        {
            return JsonSerializer.Deserialize<TbcMerchantData>(raw) ?? new TbcMerchantData(null, null);
        }
        catch (JsonException)
        {
            return new TbcMerchantData(null, null);
        }
    }
}
