using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Tracking;
using NipNip.Shared.Crypto;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.CityPay;

public class CityPayService(
    AppDbContext db,
    StoreService storeService,
    CityPayApiClient cityPayApi,
    AesStringProtector protector,
    IEmailService emailService,
    TrackingService trackingService,
    ILogger<CityPayService> logger)
{
    // --- Merchant-side connection (credentials entered once in Store Settings) ---
    //
    // Unlike Flitt/TBC/BOG, there's no cheap way to validate these credentials at connect time
    // (CityPay has no "whoami"-style endpoint — the only real proof is creating a live order),
    // so — same as FlittService — this just does basic presence checks and saves.

    public async Task ConnectAsync(string clerkUserId, ConnectCityPayRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("Customer ID is required.");
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new ArgumentException("Access token is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.CityPayCustomerId = request.CustomerId.Trim();
        store.CityPayAccessTokenEncrypted = protector.Encrypt(request.AccessToken);
        store.CityPayConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.CityPayCustomerId = null;
        store.CityPayAccessTokenEncrypted = null;
        store.CityPayConnectedAt = null;

        // Defensive: a store shouldn't keep advertising CityPay as a checkout option once its
        // credentials are gone — otherwise a real customer could pick it and hit a hard error
        // mid-checkout.
        DisableCityPayInThemeConfig(store);

        await db.SaveChangesAsync();
    }

    public async Task<CityPayStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return new CityPayStatusResponse(store.CityPayConnectedAt.HasValue, store.CityPayCustomerId, store.CityPayConnectedAt);
    }

    private static void DisableCityPayInThemeConfig(Store store)
    {
        try
        {
            if (JsonNode.Parse(store.ThemeConfig) is not JsonObject config) return;
            if (config["cityPayEnabled"]?.GetValue<bool>() != true) return;
            config["cityPayEnabled"] = false;
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
        if (store.CityPayCustomerId is null || store.CityPayAccessTokenEncrypted is null)
            throw new ConflictException("This store hasn't set up CityPay payments yet.");

        var accessToken = protector.Decrypt(store.CityPayAccessTokenEncrypted);

        // order_token doubles as our own lookup key on callback — no CityPay-side value is
        // needed for that, since we control this token ourselves (unlike Tbc's/Bog's opaque
        // gateway-issued ids).
        var orderToken = order.Id.ToString("N");

        var result = await cityPayApi.CreateOrderAsync(store.CityPayCustomerId, accessToken, orderToken, orderToken, order.Total);

        order.CityPayOrderId = result.OrderId;
        order.CityPayMerchantData = merchantData;
        await db.SaveChangesAsync();

        return result.PaymentUrl;
    }

    // --- Callback handling ---
    //
    // CityPay's callback carries no signature at all (their suggested trust mechanism is an IP
    // allowlist instead), so rather than trust the callback body's own status field, this only
    // reads the order_token out of it as a "check now" trigger and re-pulls the real status via
    // CityPay's no-auth "get order" endpoint — the same notify-then-pull shape as Tbc/BogService,
    // chosen for the same reason (no way to cryptographically trust the callback payload).

    private record CityPayMerchantData(string? SessionId, string? Ref);

    public async Task HandleCallbackAsync(string orderToken)
    {
        if (!Guid.TryParseExact(orderToken, "N", out var orderId))
        {
            logger.LogWarning("CityPay callback with unparseable order_token {OrderToken}.", orderToken);
            return;
        }

        var order = await db.Orders
            .Include(o => o.Store)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        if (order is null)
        {
            logger.LogWarning("CityPay callback for unknown order {OrderId}.", orderId);
            return;
        }

        var store = order.Store;
        string statusCode;
        try
        {
            var result = await cityPayApi.GetOrderStatusAsync(orderToken);
            statusCode = result.StatusCode.ToUpperInvariant();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch CityPay order status for {OrderToken}.", orderToken);
            return;
        }

        var wasAlreadyConfirmed = order.Status == OrderStatus.Confirmed;

        switch (statusCode)
        {
            // Overpaid is treated as fully paid — no refund chasing over pocket change from
            // exchange-rate drift between page load and the buyer's transaction confirming.
            case "CONFIRMED":
            case "OVERPAID":
                order.Status = OrderStatus.Confirmed;
                order.PaymentConfirmedAt ??= DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();

                if (!wasAlreadyConfirmed)
                    await FinalizeApprovedOrderAsync(order, store);
                break;

            case "EXPIRED":
            case "CANCELED":
            case "CANCELLED":
                order.Status = OrderStatus.Cancelled;
                await db.SaveChangesAsync();
                break;

            default:
                // IN_PROGRESS / PAID_UNCONFIRMED / UNDERPAID / anything unrecognized — leave
                // Pending. Underpaid just waits for a top-up or expires like any unpaid order.
                break;
        }
    }

    private async Task FinalizeApprovedOrderAsync(Order order, Store store)
    {
        var merchantData = ParseMerchantData(order.CityPayMerchantData);

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
                logger.LogError(ex, "Failed to record storefront affiliate conversion for CityPay order {OrderId}", order.Id);
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
            logger.LogError(ex, "Failed to send order confirmation email for CityPay order {OrderId}", order.Id);
        }
    }

    private static CityPayMerchantData ParseMerchantData(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return new CityPayMerchantData(null, null);
        try
        {
            return JsonSerializer.Deserialize<CityPayMerchantData>(raw) ?? new CityPayMerchantData(null, null);
        }
        catch (JsonException)
        {
            return new CityPayMerchantData(null, null);
        }
    }
}
