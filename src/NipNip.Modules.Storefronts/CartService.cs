using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Modules.Tracking;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class CartService(AppDbContext db, IEmailService emailService, TrackingService trackingService, ILogger<CartService> logger)
{
    public async Task<CartResponse> GetCartAsync(string slug, string? sessionId)
    {
        var cart = await GetOrCreateCartAsync(slug, sessionId);
        return cart.ToDto();
    }

    public async Task<CartResponse> AddItemAsync(string slug, string? sessionId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var variant = await db.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .Include(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.Product.StoreId == cart.StoreId)
            ?? throw new NotFoundException("Product variant not found.");

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == variant.Id);
        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
        }
        else
        {
            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                VariantId = variant.Id,
                Variant = variant,
                Quantity = request.Quantity,
            };
            db.CartItems.Add(newItem);
        }

        await db.SaveChangesAsync();
        return cart.ToDto();
    }

    public async Task<CartResponse> UpdateItemAsync(string slug, string? sessionId, Guid itemId, UpdateCartItemRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync();

        return cart.ToDto();
    }

    public async Task<CartResponse> RemoveItemAsync(string slug, string? sessionId, Guid itemId)
    {
        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var item = cart.Items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart item not found.");

        cart.Items.Remove(item);
        db.CartItems.Remove(item);
        await db.SaveChangesAsync();

        return cart.ToDto();
    }

    public async Task<OrderResponse> CheckoutAsync(string slug, string? sessionId, CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ArgumentException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new ArgumentException("A valid email is required.");

        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ArgumentException("Phone is required.");

        if (string.IsNullOrWhiteSpace(request.Address))
            throw new ArgumentException("Address is required.");

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            throw new ArgumentException("Payment method must be 'CashOnDelivery' or 'BankTransfer'.");

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        if (cart.Items.Count == 0)
            throw new ArgumentException("Cart is empty.");

        var store = await db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == cart.StoreId);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = cart.StoreId,
            CustomerName = request.CustomerName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PaymentMethod = paymentMethod,
            Status = OrderStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var orderItems = cart.Items.Select(i => new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            VariantId = i.VariantId,
            Quantity = i.Quantity,
            PriceAtPurchase = i.Variant.SalePrice ?? i.Variant.Price,
        }).ToList();

        var subtotal = orderItems.Sum(i => i.PriceAtPurchase * i.Quantity);

        var (shippingFee, shippingZoneName) = store is not null
            ? ExtractShipping(store.ThemeConfig, request.ShippingZoneId, subtotal)
            : (0m, null);

        order.ShippingFee = shippingFee;
        order.ShippingZoneName = shippingZoneName;
        order.Total = subtotal + shippingFee;

        var emailItems = cart.Items.Select(i => new OrderConfirmationEmailItem(
            i.Variant.Product.Name,
            i.Quantity,
            i.Variant.SalePrice ?? i.Variant.Price
        )).ToList();

        db.Orders.Add(order);
        db.OrderItems.AddRange(orderItems);
        db.CartItems.RemoveRange(cart.Items);

        await db.SaveChangesAsync();

        if (store is { AffiliateEnabled: true })
        {
            try
            {
                await trackingService.TrackStorefrontConversionAsync(
                    store.MerchantId, request.Ref, order.Id.ToString(), order.Total, "GEL");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to record storefront affiliate conversion for order {OrderId}", order.Id);
            }
        }

        if (store is not null)
        {
            try
            {
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
                    ExtractPaymentNotes(store.ThemeConfig, order.PaymentMethod)
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send order confirmation email to {Email}", order.Email);
            }
        }

        return order.ToDto();
    }

    private static string? ExtractPaymentNotes(string themeConfigJson, PaymentMethod method)
    {
        try
        {
            using var doc = JsonDocument.Parse(themeConfigJson);
            var key = method == PaymentMethod.BankTransfer ? "bankTransferNotes" : "codNotes";
            if (doc.RootElement.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                return prop.GetString() is { Length: > 0 } value ? value : null;
            }
        }
        catch (JsonException)
        {
        }
        return null;
    }

    private static (decimal Fee, string? ZoneName) ExtractShipping(string themeConfigJson, string? shippingZoneId, decimal subtotal)
    {
        try
        {
            using var doc = JsonDocument.Parse(themeConfigJson);

            if (!doc.RootElement.TryGetProperty("shippingZones", out var zonesProp) || zonesProp.ValueKind != JsonValueKind.Array)
                return (0m, null);

            var zones = zonesProp.EnumerateArray().ToList();
            if (zones.Count == 0)
                return (0m, null);

            if (string.IsNullOrWhiteSpace(shippingZoneId))
                throw new ArgumentException("Please select a delivery area.");

            var zone = zones.FirstOrDefault(z =>
                z.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String && idProp.GetString() == shippingZoneId);

            if (zone.ValueKind == JsonValueKind.Undefined)
                throw new ArgumentException("Selected delivery area is not available.");

            var name = zone.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                ? nameProp.GetString()
                : null;
            var price = zone.TryGetProperty("price", out var priceProp) && priceProp.TryGetDecimal(out var p) ? p : 0m;

            var freeThreshold = doc.RootElement.TryGetProperty("freeShippingThreshold", out var thresholdProp) && thresholdProp.ValueKind == JsonValueKind.Number
                ? thresholdProp.GetDecimal()
                : (decimal?)null;

            var fee = freeThreshold.HasValue && subtotal >= freeThreshold.Value ? 0m : price;

            return (fee, name);
        }
        catch (JsonException)
        {
            return (0m, null);
        }
    }

    private async Task<Cart> GetOrCreateCartAsync(string slug, string? sessionId)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        Cart? cart = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            cart = await db.Carts
                .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
                .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
                .FirstOrDefaultAsync(c => c.StoreId == store.Id && c.SessionId == sessionId);
        }

        if (cart is not null) return cart;

        cart = new Cart
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            SessionId = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Carts.Add(cart);
        await db.SaveChangesAsync();

        return cart;
    }
}
