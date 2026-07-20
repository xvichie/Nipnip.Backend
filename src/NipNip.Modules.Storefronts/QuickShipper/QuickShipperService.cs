using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Crypto;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.QuickShipper;

public class QuickShipperService(
    AppDbContext db,
    StoreService storeService,
    QuickShipperAuthClient authClient,
    QuickShipperOrderClient orderClient,
    AesStringProtector protector)
{
    // QuickShipper is Georgia-only today (per its docs' Tbilisi examples) — merchants enter
    // local numbers without a prefix, so this is fixed rather than a per-store setting.
    private const string PhonePrefix = "995";
    private const int TokenExpirySafetyBufferSeconds = 60;

    // --- Connection ---

    public async Task<QuickShipperStatusResponse> ConnectAsync(string clerkUserId, ConnectQuickShipperRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Username and password are required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        // Validate immediately so a typo surfaces here, not the next time the merchant tries to ship an order.
        var token = await authClient.RequestTokenAsync(request.Username, request.Password);

        store.QuickShipperUsernameEncrypted = protector.Encrypt(request.Username);
        store.QuickShipperPasswordEncrypted = protector.Encrypt(request.Password);
        store.QuickShipperAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
        store.QuickShipperTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
        store.QuickShipperConnectedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return await GetStatusAsync(clerkUserId);
    }

    public async Task DisconnectAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.QuickShipperUsernameEncrypted = null;
        store.QuickShipperPasswordEncrypted = null;
        store.QuickShipperAccessTokenEncrypted = null;
        store.QuickShipperTokenExpiresAt = null;
        store.QuickShipperConnectedAt = null;
        await db.SaveChangesAsync();
    }

    public async Task<QuickShipperStatusResponse> GetStatusAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var hasPickup = store.PickupAddress is not null && store.PickupLatitude.HasValue && store.PickupLongitude.HasValue;
        return new QuickShipperStatusResponse(store.QuickShipperConnectedAt.HasValue, store.QuickShipperConnectedAt, hasPickup);
    }

    // --- Pickup location ---

    public async Task<PickupLocationResponse> GetPickupLocationAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return ToPickupLocationDto(store);
    }

    public async Task<PickupLocationResponse> SavePickupLocationAsync(string clerkUserId, SavePickupLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Address)) throw new ArgumentException("Address is required.");
        if (string.IsNullOrWhiteSpace(request.ContactName)) throw new ArgumentException("Contact name is required.");
        if (string.IsNullOrWhiteSpace(request.Phone)) throw new ArgumentException("Phone is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        store.PickupAddress = request.Address.Trim();
        store.PickupLatitude = request.Latitude;
        store.PickupLongitude = request.Longitude;
        store.PickupContactName = request.ContactName.Trim();
        store.PickupPhone = request.Phone.Trim();
        await db.SaveChangesAsync();

        return ToPickupLocationDto(store);
    }

    private static PickupLocationResponse ToPickupLocationDto(Store store) =>
        new(store.PickupAddress, store.PickupLatitude, store.PickupLongitude, store.PickupContactName, store.PickupPhone);

    // --- Custom fields ---

    public async Task<List<QuickShipperCustomFieldResponse>> GetCustomFieldsAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var token = await GetAccessTokenAsync(store);
        var node = await orderClient.GetCustomFieldsAsync(token);

        var result = new List<QuickShipperCustomFieldResponse>();
        if (node["items"] is JsonArray items)
        {
            foreach (var item in items)
            {
                if (item is null) continue;
                List<string>? listValues = item["listValues"] is JsonArray lv
                    ? lv.Select(v => v!.GetValue<string>()).ToList()
                    : null;

                result.Add(new QuickShipperCustomFieldResponse(
                    item["id"]!.GetValue<int>(),
                    item["name"]?.GetValue<string>(),
                    item["isOptional"]?.GetValue<bool>() ?? true,
                    item["description"]?.GetValue<string>(),
                    item["placeholder"]?.GetValue<string>(),
                    listValues,
                    item["type"]?.GetValue<string>(),
                    item["valueType"]?.GetValue<string>()
                ));
            }
        }

        return result;
    }

    // --- Fees ---

    public async Task<QuickShipperFeesResponse> GetFeesForOrderAsync(string clerkUserId, Guid orderId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        EnsurePickupLocationSet(store);
        var order = await GetOwnOrderAsync(store.Id, orderId);
        EnsureOrderIsGeocoded(order);

        var token = await GetAccessTokenAsync(store);
        var query = new QuickShipperFeesQuery(
            store.PickupAddress!, store.PickupLatitude!.Value, store.PickupLongitude!.Value,
            order.Address, order.Latitude!.Value, order.Longitude!.Value,
            order.Total);

        var node = await orderClient.GetFeesAsync(token, query);

        var options = new List<QuickShipperFeeOptionResponse>();
        if (node["fees"] is JsonArray fees)
        {
            foreach (var fee in fees)
            {
                if (fee is null) continue;
                var providerId = fee["providerId"]!.GetValue<int>();
                var providerName = fee["providerName"]?.GetValue<string>();
                var providerLogoUrl = fee["providerLogoUrl"]?.GetValue<string>();
                var isActive = fee["isActive"]?.GetValue<bool>() ?? true;
                var hasCod = fee["hasCashOnDelivery"]?.GetValue<bool>() ?? false;

                if (fee["prices"] is not JsonArray prices) continue;
                foreach (var price in prices)
                {
                    if (price is null) continue;
                    options.Add(new QuickShipperFeeOptionResponse(
                        providerId,
                        providerName,
                        providerLogoUrl,
                        (decimal)(price["amount"]?.GetValue<double>() ?? 0),
                        price["currency"]?.GetValue<string>(),
                        price["deliverySpeedName"]?.GetValue<string>(),
                        price["id"]?.GetValue<string>(),
                        hasCod,
                        isActive
                    ));
                }
            }
        }

        var distance = node["distance"]?.GetValue<double>() ?? 0;
        return new QuickShipperFeesResponse(options, distance);
    }

    // --- Order creation / status ---

    public async Task<OrderDetailResponse> CreateDeliveryOrderAsync(string clerkUserId, Guid orderId, CreateQuickShipperOrderRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        EnsurePickupLocationSet(store);
        var order = await GetOwnOrderAsync(store.Id, orderId);
        EnsureOrderIsGeocoded(order);

        if (order.QuickShipperOrderId.HasValue)
            throw new ConflictException("A QuickShipper delivery already exists for this order.");

        var token = await GetAccessTokenAsync(store);

        var body = new
        {
            carDelivery = false,
            dropOffInfo = new
            {
                address = order.Address,
                longitude = order.Longitude!.Value,
                latitude = order.Latitude!.Value,
                name = order.CustomerName,
                phonePrefix = PhonePrefix,
                phone = order.Phone,
            },
            pickUpInfo = new
            {
                address = store.PickupAddress,
                longitude = store.PickupLongitude!.Value,
                latitude = store.PickupLatitude!.Value,
                name = store.PickupContactName,
                phonePrefix = PhonePrefix,
                phone = store.PickupPhone,
            },
            provider = new { providerId = request.ProviderId, providerFeeId = request.PriceId },
            generalFields = (request.CustomFieldValues ?? [])
                .Select(f => new { id = f.Id, value = f.Value, type = f.Type })
                .ToList(),
            autoAssign = true,
            integrationOrderId = order.Id.ToString(),
            dropThePin = false,
            cashOnDelivery = order.PaymentMethod == PaymentMethod.CashOnDelivery
                ? new { deliveryPrice = (decimal?)null, parcelPrice = (decimal?)order.Total }
                : null,
            cartAmount = order.Total,
        };

        var result = await orderClient.CreateOrderAsync(token, body);

        order.QuickShipperOrderId = result["orderId"]!.GetValue<int>();
        order.QuickShipperStatus = result["orderStatus"]?.GetValue<string>();
        order.QuickShipperTrackingUrl = result["trackingUrl"]?.GetValue<string>();
        order.QuickShipperDeliveryFee = (decimal)(result["deliveryFee"]?.GetValue<double>() ?? 0);
        await db.SaveChangesAsync();

        return order.ToDetailDto();
    }

    public async Task<OrderDetailResponse> RefreshOrderStatusAsync(string clerkUserId, Guid orderId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var order = await GetOwnOrderAsync(store.Id, orderId);

        if (!order.QuickShipperOrderId.HasValue)
            throw new ConflictException("This order hasn't been shipped with QuickShipper yet.");

        var token = await GetAccessTokenAsync(store);
        var result = await orderClient.GetOrderInfoAsync(token, order.QuickShipperOrderId.Value);

        order.QuickShipperStatus = result["order"]?["status"]?.GetValue<string>() ?? order.QuickShipperStatus;
        order.QuickShipperTrackingUrl = result["order"]?["trackingUrl"]?.GetValue<string>() ?? order.QuickShipperTrackingUrl;
        await db.SaveChangesAsync();

        return order.ToDetailDto();
    }

    // --- Helpers ---

    private async Task<string> GetAccessTokenAsync(Store store)
    {
        if (store.QuickShipperUsernameEncrypted is null || store.QuickShipperPasswordEncrypted is null)
            throw new ConflictException("Connect your QuickShipper account first.");

        if (store.QuickShipperAccessTokenEncrypted is not null
            && store.QuickShipperTokenExpiresAt.HasValue
            && store.QuickShipperTokenExpiresAt.Value > DateTimeOffset.UtcNow.AddSeconds(TokenExpirySafetyBufferSeconds))
        {
            return protector.Decrypt(store.QuickShipperAccessTokenEncrypted);
        }

        var username = protector.Decrypt(store.QuickShipperUsernameEncrypted);
        var password = protector.Decrypt(store.QuickShipperPasswordEncrypted);
        var token = await authClient.RequestTokenAsync(username, password);

        store.QuickShipperAccessTokenEncrypted = protector.Encrypt(token.AccessToken);
        store.QuickShipperTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresInSeconds);
        await db.SaveChangesAsync();

        return token.AccessToken;
    }

    private static void EnsurePickupLocationSet(Store store)
    {
        if (store.PickupAddress is null || !store.PickupLatitude.HasValue || !store.PickupLongitude.HasValue)
            throw new ConflictException("Set your pickup location in Store Settings first.");
    }

    private static void EnsureOrderIsGeocoded(Order order)
    {
        if (!order.Latitude.HasValue || !order.Longitude.HasValue)
            throw new ConflictException("This order has no map location on file, so a precise delivery route can't be calculated.");
    }

    // Includes mirror OrderService.OrdersWithIncludes — ToDetailDto() needs Items/Notes
    // populated, and this service returns the mapped OrderDetailResponse after every mutation.
    private async Task<Order> GetOwnOrderAsync(Guid storeId, Guid orderId)
    {
        return await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
            .Include(o => o.Notes)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.StoreId == storeId)
            ?? throw new NotFoundException("Order not found.");
    }
}
