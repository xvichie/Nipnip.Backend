using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Bog;
using NipNip.Modules.Storefronts.CityPay;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Modules.Storefronts.Flitt;
using NipNip.Modules.Storefronts.Tbc;
using NipNip.Modules.Tracking;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class CartService(
    AppDbContext db,
    IEmailService emailService,
    TrackingService trackingService,
    FlittService flittService,
    TbcService tbcService,
    BogService bogService,
    CityPayService cityPayService,
    StoreDiscountCodeService discountCodeService,
    ILogger<CartService> logger)
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

        var variant = await ResolveVariantAsync(cart.StoreId, request.ProductId, request.OptionValueIds);

        var existingItem = cart.Items.FirstOrDefault(i => i.VariantId == variant.Id);
        var newQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

        if (variant.Stock is { } stock && newQuantity > stock)
            throw new ArgumentException("Not enough stock available.");

        if (existingItem is not null)
        {
            existingItem.Quantity = newQuantity;
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

    /// <summary>
    /// Finds the variant matching the given option-value combination (or the product's sole
    /// variant for products with no options). If no explicit override exists for that
    /// combination, materializes a default variant on the spot at the product's base price
    /// with unlimited stock — it becomes an ordinary, editable variant from that point on.
    /// </summary>
    private async Task<ProductVariant> ResolveVariantAsync(Guid storeId, Guid productId, List<Guid> optionValueIds)
    {
        var product = await db.Products
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == storeId)
            ?? throw new NotFoundException("Product not found.");

        var configuredOptions = product.Options.Where(o => o.Values.Count > 0).ToList();
        var selectedIds = optionValueIds.Distinct().ToList();

        if (configuredOptions.Count > 0)
        {
            var validCount = configuredOptions.SelectMany(o => o.Values).Count(v => selectedIds.Contains(v.Id));
            var optionIdsCovered = configuredOptions
                .Where(o => o.Values.Any(v => selectedIds.Contains(v.Id)))
                .Select(o => o.Id)
                .Distinct()
                .Count();

            if (validCount != selectedIds.Count || optionIdsCovered != configuredOptions.Count || selectedIds.Count != configuredOptions.Count)
                throw new ArgumentException("Select exactly one value for every product option.");
        }
        else if (selectedIds.Count > 0)
        {
            throw new ArgumentException("This product has no selectable options.");
        }

        var existing = product.Variants.FirstOrDefault(v =>
        {
            var variantValueIds = v.OptionValues.Select(ov => ov.OptionValueId).ToHashSet();
            return variantValueIds.SetEquals(selectedIds);
        });

        var variantId = existing?.Id;

        if (variantId is null)
        {
            var defaultVariant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Sku = $"AUTO-{Guid.NewGuid():N}"[..13],
                Price = product.BasePrice,
                SalePrice = product.SalePrice,
                Stock = null,
            };
            db.ProductVariants.Add(defaultVariant);

            foreach (var valueId in selectedIds)
            {
                db.ProductVariantOptionValues.Add(new ProductVariantOptionValue
                {
                    VariantId = defaultVariant.Id,
                    OptionValueId = valueId,
                });
            }

            await db.SaveChangesAsync();
            variantId = defaultVariant.Id;
        }

        return await db.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .Include(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
            .FirstAsync(v => v.Id == variantId);
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

    public async Task<CartResponse> AddBundleToCartAsync(string slug, string? sessionId, AddBundleToCartRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var bundle = await db.ProductBundles
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Variants)
            .FirstOrDefaultAsync(b => b.Id == request.BundleId && b.StoreId == cart.StoreId && b.IsActive)
            ?? throw new NotFoundException("Bundle not found.");

        var existingItem = cart.BundleItems.FirstOrDefault(i => i.BundleId == bundle.Id);
        var newBundleQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;

        EnsureBundleStockAvailable(bundle, newBundleQuantity);

        if (existingItem is not null)
        {
            existingItem.Quantity = newBundleQuantity;
        }
        else
        {
            var newItem = new CartBundleItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                BundleId = bundle.Id,
                Bundle = bundle,
                Quantity = request.Quantity,
            };
            db.CartBundleItems.Add(newItem);
        }

        await db.SaveChangesAsync();
        return cart.ToDto();
    }

    /// <summary>
    /// Bundles don't collect option selections the way regular add-to-cart does, so there's no
    /// single variant to check — this sums stock across all of a product's variants (treating any
    /// variant with unlimited stock as making the whole product unlimited) as a best-effort floor,
    /// mainly to catch the common case of a bundle component that's fully sold out.
    /// </summary>
    private static void EnsureBundleStockAvailable(ProductBundle bundle, int bundleQuantity)
    {
        foreach (var item in bundle.Items)
        {
            var variants = item.Product.Variants;
            if (variants.Count == 0) continue; // no variants materialized yet — nothing sold against this product yet

            if (variants.Any(v => v.Stock is null)) continue; // unlimited stock

            var availableStock = variants.Sum(v => v.Stock ?? 0);
            var requiredStock = item.Quantity * bundleQuantity;

            if (availableStock < requiredStock)
                throw new ArgumentException($"'{item.Product.Name}' doesn't have enough stock for this bundle quantity.");
        }
    }

    public async Task<CartResponse> UpdateBundleItemAsync(string slug, string? sessionId, Guid itemId, UpdateCartBundleItemRequest request)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var item = cart.BundleItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart bundle item not found.");

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync();

        return cart.ToDto();
    }

    public async Task<CartResponse> RemoveBundleItemAsync(string slug, string? sessionId, Guid itemId)
    {
        var cart = await GetOrCreateCartAsync(slug, sessionId);

        var item = cart.BundleItems.FirstOrDefault(i => i.Id == itemId)
            ?? throw new NotFoundException("Cart bundle item not found.");

        cart.BundleItems.Remove(item);
        db.CartBundleItems.Remove(item);
        await db.SaveChangesAsync();

        return cart.ToDto();
    }

    public async Task<OrderResponse> CheckoutAsync(string slug, string? sessionId, CheckoutRequest request, OrderSource source = OrderSource.Storefront)
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
            throw new ArgumentException("Payment method must be 'CashOnDelivery', 'BankTransfer', 'Flitt', 'Tbc', 'Bog', or 'CityPay'.");

        var isHostedCheckout = paymentMethod is PaymentMethod.Flitt or PaymentMethod.Tbc or PaymentMethod.Bog or PaymentMethod.CityPay;

        var cart = await GetOrCreateCartAsync(slug, sessionId);

        if (cart.Items.Count == 0 && cart.BundleItems.Count == 0)
            throw new ArgumentException("Cart is empty.");

        var store = await db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Id == cart.StoreId);

        if (isHostedCheckout && store is null)
            throw new NotFoundException("Store not found.");

        if (store is not null && !IsPaymentMethodEnabled(store.ThemeConfig, paymentMethod))
            throw new ArgumentException("This payment method is not available for this store.");

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
            CustomerNote = string.IsNullOrWhiteSpace(request.CustomerNote) ? null : request.CustomerNote.Trim(),
            PaymentMethod = paymentMethod,
            IsPickup = request.IsPickup,
            Status = OrderStatus.Pending,
            Source = source,
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

        var orderBundleItems = cart.BundleItems.Select(i => new OrderBundleItem
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            BundleId = i.BundleId,
            Quantity = i.Quantity,
            PriceAtPurchase = i.Bundle.BundlePrice,
        }).ToList();

        var subtotal = orderItems.Sum(i => i.PriceAtPurchase * i.Quantity)
            + orderBundleItems.Sum(i => i.PriceAtPurchase * i.Quantity);

        var minOrderAmount = store is not null ? ExtractMinOrderAmount(store.ThemeConfig) : null;
        if (minOrderAmount is { } min && subtotal < min)
            throw new ArgumentException($"Minimum order amount is {min:F2}.");

        var (shippingFee, shippingZoneName) = store is not null && !request.IsPickup
            ? ExtractShipping(store.ThemeConfig, request.ShippingZoneId, subtotal)
            : (0m, null);

        var (discountAmount, appliedDiscountCode) = await discountCodeService.PreviewForCheckoutAsync(
            cart.StoreId, request.DiscountCode, subtotal);

        order.ShippingFee = shippingFee;
        order.ShippingZoneName = shippingZoneName;
        order.DiscountCode = appliedDiscountCode;
        order.DiscountAmount = discountAmount;
        order.Total = subtotal + shippingFee - discountAmount;

        var emailItems = cart.Items.Select(i => new OrderConfirmationEmailItem(
            i.Variant.Product.Name,
            i.Quantity,
            i.Variant.SalePrice ?? i.Variant.Price
        )).Concat(cart.BundleItems.Select(i => new OrderConfirmationEmailItem(
            i.Bundle.Name,
            i.Quantity,
            i.Bundle.BundlePrice
        ))).ToList();

        db.Orders.Add(order);
        db.OrderItems.AddRange(orderItems);
        db.OrderBundleItems.AddRange(orderBundleItems);

        // Flitt/TBC/BOG/CityPay orders aren't "placed" yet — the customer still has to complete
        // a hosted payment. The cart, confirmation email, affiliate conversion, and discount-code
        // usage all wait until the callback confirms payment (each gateway's own
        // FinalizeApprovedOrderAsync), so an abandoned/declined payment doesn't lose the
        // customer's cart, fire a false conversion, or burn a limited-run discount code for a
        // sale that never happened.
        if (!isHostedCheckout)
        {
            db.CartItems.RemoveRange(cart.Items);
            db.CartBundleItems.RemoveRange(cart.BundleItems);
        }

        await db.SaveChangesAsync();

        if (!isHostedCheckout && appliedDiscountCode is not null)
        {
            if (!await discountCodeService.ConfirmUsageAsync(cart.StoreId, appliedDiscountCode))
                logger.LogWarning("Discount code {Code} could not be confirmed for order {OrderId} — it likely hit its usage limit concurrently.", appliedDiscountCode, order.Id);
        }

        if (paymentMethod == PaymentMethod.Flitt)
        {
            var merchantData = JsonSerializer.Serialize(new { sessionId = cart.SessionId, request.Ref });
            var checkoutUrl = await flittService.CreateCheckoutSessionAsync(order, store!, merchantData);
            return order.ToDto() with { RedirectUrl = checkoutUrl };
        }

        if (paymentMethod == PaymentMethod.Tbc)
        {
            var merchantData = JsonSerializer.Serialize(new { sessionId = cart.SessionId, request.Ref });
            var checkoutUrl = await tbcService.CreateCheckoutSessionAsync(order, store!, merchantData);
            return order.ToDto() with { RedirectUrl = checkoutUrl };
        }

        if (paymentMethod == PaymentMethod.Bog)
        {
            var merchantData = JsonSerializer.Serialize(new { sessionId = cart.SessionId, request.Ref });
            var checkoutUrl = await bogService.CreateCheckoutSessionAsync(order, store!, merchantData);
            return order.ToDto() with { RedirectUrl = checkoutUrl };
        }

        if (paymentMethod == PaymentMethod.CityPay)
        {
            var merchantData = JsonSerializer.Serialize(new { sessionId = cart.SessionId, request.Ref });
            var checkoutUrl = await cityPayService.CreateCheckoutSessionAsync(order, store!, merchantData);
            return order.ToDto() with { RedirectUrl = checkoutUrl };
        }

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

    public async Task<OrderResponse> GetOrderStatusAsync(string slug, Guid orderId)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.Store.Slug == slug)
            ?? throw new NotFoundException("Order not found.");

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

    private static bool IsPaymentMethodEnabled(string themeConfigJson, PaymentMethod method)
    {
        var key = method switch
        {
            PaymentMethod.CashOnDelivery => "codEnabled",
            PaymentMethod.BankTransfer => "bankTransferEnabled",
            PaymentMethod.Flitt => "flittEnabled",
            PaymentMethod.Tbc => "tbcEnabled",
            PaymentMethod.Bog => "bogEnabled",
            PaymentMethod.CityPay => "cityPayEnabled",
            _ => null,
        };
        if (key is null) return false;

        // Mirrors the frontend's DEFAULT_THEME_CONFIG: a store that has never touched its
        // ThemeConfig ships with COD and bank transfer enabled and everything else off, since
        // the backend stores "{}" for a brand-new store rather than the full default blob.
        var defaultEnabled = method is PaymentMethod.CashOnDelivery or PaymentMethod.BankTransfer;

        try
        {
            using var doc = JsonDocument.Parse(themeConfigJson);
            if (doc.RootElement.TryGetProperty(key, out var prop) &&
                (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
            {
                return prop.GetBoolean();
            }
        }
        catch (JsonException)
        {
        }
        return defaultEnabled;
    }

    private static decimal? ExtractMinOrderAmount(string themeConfigJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(themeConfigJson);
            return doc.RootElement.TryGetProperty("minOrderAmount", out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetDecimal()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
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
                .Include(c => c.BundleItems).ThenInclude(i => i.Bundle)
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
