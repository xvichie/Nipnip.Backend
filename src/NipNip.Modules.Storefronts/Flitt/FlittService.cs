using System.Globalization;
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

namespace NipNip.Modules.Storefronts.Flitt;

public class FlittService(
    AppDbContext db,
    StoreService storeService,
    FlittApiClient flittApi,
    AesStringProtector protector,
    IEmailService emailService,
    TrackingService trackingService,
    StoreDiscountCodeService discountCodeService,
    IOptions<FlittOptions> options,
    ILogger<FlittService> logger)
{
    private readonly FlittOptions _options = options.Value;

    // --- Merchant-side connection (credentials entered once in Store Settings) ---

    public async Task ConnectAsync(string clerkUserId, ConnectFlittRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.MerchantId) || !long.TryParse(request.MerchantId.Trim(), out _))
            throw new ArgumentException("Merchant ID must be numeric.");
        if (string.IsNullOrWhiteSpace(request.SecretKey))
            throw new ArgumentException("Secret key is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.FlittMerchantId = request.MerchantId.Trim();
        store.FlittSecretKeyEncrypted = protector.Encrypt(request.SecretKey);
        store.FlittConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.FlittMerchantId = null;
        store.FlittSecretKeyEncrypted = null;
        store.FlittConnectedAt = null;

        // Defensive: a store shouldn't keep advertising Flitt as a checkout option once its
        // credentials are gone — otherwise a real customer could pick "pay by card" and hit
        // a hard error mid-checkout.
        DisableFlittInThemeConfig(store);

        await db.SaveChangesAsync();
    }

    public async Task<FlittStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new FlittStatusResponse(store.FlittConnectedAt.HasValue, store.FlittMerchantId, store.FlittConnectedAt);
    }

    private static void DisableFlittInThemeConfig(Store store)
    {
        try
        {
            if (JsonNode.Parse(store.ThemeConfig) is not JsonObject config) return;
            if (config["flittEnabled"]?.GetValue<bool>() != true) return;
            config["flittEnabled"] = false;
            store.ThemeConfig = config.ToJsonString();
        }
        catch (JsonException)
        {
            // Malformed theme config isn't this method's concern to fix.
        }
    }

    // --- Checkout session creation (called from CartService.CheckoutAsync) ---

    public async Task<string> CreateCheckoutSessionAsync(Order order, Store store, string merchantData)
    {
        if (store.FlittMerchantId is null || store.FlittSecretKeyEncrypted is null)
            throw new ConflictException("This store hasn't set up card payments yet.");

        var secretKey = protector.Decrypt(store.FlittSecretKeyEncrypted);
        var orderIdString = order.Id.ToString("N");
        var amountMinorUnits = (long)Math.Round(order.Total * 100, MidpointRounding.AwayFromZero);
        var orderDesc = $"Order at {store.Name}";
        var responseUrl = $"{_options.FrontendUrl.TrimEnd('/')}/store/{store.Slug}/checkout/confirmation?orderId={order.Id}";
        var callbackUrl = $"{_options.ApiBaseUrl.TrimEnd('/')}/api/webhooks/flitt";

        var signatureFields = new Dictionary<string, string?>
        {
            ["order_id"] = orderIdString,
            ["merchant_id"] = store.FlittMerchantId,
            ["order_desc"] = orderDesc,
            ["amount"] = amountMinorUnits.ToString(CultureInfo.InvariantCulture),
            ["currency"] = "GEL",
            ["response_url"] = responseUrl,
            ["server_callback_url"] = callbackUrl,
            ["merchant_data"] = merchantData,
        };
        var signature = FlittSignature.Build(secretKey, signatureFields);

        var requestFields = new Dictionary<string, object?>
        {
            ["order_id"] = orderIdString,
            ["merchant_id"] = long.Parse(store.FlittMerchantId, CultureInfo.InvariantCulture),
            ["order_desc"] = orderDesc,
            ["amount"] = amountMinorUnits,
            ["currency"] = "GEL",
            ["response_url"] = responseUrl,
            ["server_callback_url"] = callbackUrl,
            ["merchant_data"] = merchantData,
            ["signature"] = signature,
        };

        return await flittApi.CreateCheckoutUrlAsync(requestFields);
    }

    // --- Callback handling (server_callback_url, verified + idempotent) ---

    private record FlittMerchantData(string? SessionId, string? Ref, string? Lang = null);

    public async Task HandleCallbackAsync(JsonNode payload)
    {
        var merchantId = GetField(payload, "merchant_id");
        if (merchantId is null)
        {
            logger.LogWarning("Flitt callback missing merchant_id.");
            return;
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.FlittMerchantId == merchantId);
        if (store?.FlittSecretKeyEncrypted is null)
        {
            logger.LogWarning("Flitt callback for unknown merchant_id {MerchantId}.", merchantId);
            return;
        }

        if (!FlittSignature.Verify(protector.Decrypt(store.FlittSecretKeyEncrypted), payload))
        {
            logger.LogWarning("Flitt callback with invalid signature for store {StoreId}.", store.Id);
            return;
        }

        var orderIdRaw = GetField(payload, "order_id");
        if (orderIdRaw is null || !Guid.TryParseExact(orderIdRaw, "N", out var orderId))
        {
            logger.LogWarning("Flitt callback with unparseable order_id {OrderId}.", orderIdRaw);
            return;
        }

        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .Include(o => o.BundleItems).ThenInclude(i => i.Bundle)
            .Include(o => o.BundleItems).ThenInclude(i => i.StockAllocations)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StoreId == store.Id);
        if (order is null)
        {
            logger.LogWarning("Flitt callback for unknown order {OrderId}.", orderId);
            return;
        }

        var paymentIdRaw = GetField(payload, "payment_id");
        if (paymentIdRaw is not null && long.TryParse(paymentIdRaw, out var paymentId))
            order.FlittPaymentId = paymentId;

        var orderStatus = GetField(payload, "order_status");
        var wasAlreadyConfirmed = order.Status == OrderStatus.Confirmed;

        switch (orderStatus)
        {
            case "approved":
                order.Status = OrderStatus.Confirmed;
                order.PaymentConfirmedAt ??= DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                // Flitt retries callbacks it doesn't get a fast 200 for — this guard keeps a
                // duplicate "approved" delivery from double-sending the email or double-
                // counting the affiliate conversion.
                if (!wasAlreadyConfirmed)
                    await FinalizeApprovedOrderAsync(order, store, GetField(payload, "merchant_data"));
                break;

            case "declined":
            case "expired":
                if (order.Status != OrderStatus.Cancelled)
                {
                    order.Status = OrderStatus.Cancelled;
                    await using var releaseTransaction = await db.Database.BeginTransactionAsync();
                    await OrderStockAdjuster.ReleaseAsync(db, order);
                    await db.SaveChangesAsync();
                    await releaseTransaction.CommitAsync();
                }
                else
                {
                    await db.SaveChangesAsync();
                }
                break;

            default:
                // "processing"/"created" — leave Pending, a later callback will follow.
                break;
        }
    }

    private async Task FinalizeApprovedOrderAsync(Order order, Store store, string? merchantDataRaw)
    {
        var merchantData = ParseMerchantData(merchantDataRaw);
        var lang = merchantData.Lang is "en" or "ru" ? merchantData.Lang : "ka";

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
                logger.LogWarning("Discount code {Code} could not be confirmed for Flitt order {OrderId} — it likely hit its usage limit concurrently.", order.DiscountCode, order.Id);
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
                logger.LogError(ex, "Failed to record storefront affiliate conversion for Flitt order {OrderId}", order.Id);
            }
        }

        try
        {
            var emailItems = order.Items
                .Select(i => new OrderConfirmationEmailItem(i.Variant.Product.DisplayName(lang), i.Quantity, i.PriceAtPurchase))
                .Concat(order.BundleItems.Select(i => new OrderConfirmationEmailItem(i.Bundle.DisplayName(lang), i.Quantity, i.PriceAtPurchase)))
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
                ExtractFlittNotes(store.ThemeConfig),
                lang
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send order confirmation email for Flitt order {OrderId}", order.Id);
        }
    }

    private static FlittMerchantData ParseMerchantData(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return new FlittMerchantData(null, null);
        try
        {
            return JsonSerializer.Deserialize<FlittMerchantData>(raw) ?? new FlittMerchantData(null, null);
        }
        catch (JsonException)
        {
            return new FlittMerchantData(null, null);
        }
    }

    private static string? ExtractFlittNotes(string themeConfigJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(themeConfigJson);
            if (doc.RootElement.TryGetProperty("flittNotes", out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString() is { Length: > 0 } value ? value : null;
        }
        catch (JsonException) { /* fall through */ }
        return null;
    }

    private static string? GetField(JsonNode payload, string key) =>
        payload[key] is JsonValue value ? FlittSignature.ExtractScalarString(value) : null;
}
