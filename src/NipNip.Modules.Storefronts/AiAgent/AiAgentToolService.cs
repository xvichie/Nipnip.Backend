using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.AiAgent;

public record ProductVariantInfo(string? OptionSummary, decimal Price, decimal? SalePrice, int? Stock, bool Available);

public record ProductLookupResult(
    bool Found,
    string? Error,
    string? Name = null,
    string? Description = null,
    string? Slug = null,
    decimal? BasePrice = null,
    decimal? SalePrice = null,
    Dictionary<string, List<string>>? Options = null,
    List<ProductVariantInfo>? Variants = null);

public record ShippingZoneInfo(string Id, string Name, decimal Price);

public record CheckoutInfoResult(
    List<ShippingZoneInfo> ShippingZones,
    decimal? FreeShippingThreshold,
    bool CashOnDeliveryEnabled,
    string? CashOnDeliveryNotes,
    bool BankTransferEnabled,
    string? BankTransferNotes);

public record DraftOrderItemInput(string ProductSlug, Dictionary<string, string> Options, int Quantity);

public record DraftOrderResult(bool Success, string? Error, Guid? OrderId = null, decimal? Total = null);

public record ProductSummary(string Name, string Slug, string Url, decimal BasePrice, decimal? SalePrice);

// Every method here is scoped to a `Store` already resolved by the caller (from the
// Page's Facebook ID) — that's what makes lookup_product's cross-store rejection work:
// a link to some other merchant's product simply doesn't resolve against this store.
public class AiAgentToolService(AppDbContext db, CartService cartService)
{
    private const string StorefrontRootDomain = "nipnip.ge";
    private const int MaxDescriptionChars = 240;

    // Catalog browsing — the other tools all need a specific product already in hand
    // (a link or a slug), so a customer asking "what do you sell?" or "any sneakers?"
    // has nothing to call without this. Returns a real storefront URL per product so
    // the agent can share it directly rather than constructing links itself.
    public async Task<List<ProductSummary>> ListProductsAsync(Store store, string? search, int limit = 5)
    {
        var query = db.Products.Where(p => p.StoreId == store.Id && p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .Select(p => new { p.Name, p.Slug, p.BasePrice, p.SalePrice })
            .ToListAsync();

        return products
            .Select(p => new ProductSummary(p.Name, p.Slug, $"https://{store.Slug}.{StorefrontRootDomain}/products/{p.Slug}", p.BasePrice, p.SalePrice))
            .ToList();
    }

    public async Task<ProductLookupResult> LookupProductAsync(Store store, string urlOrSlug)
    {
        var slug = ExtractProductSlug(store, urlOrSlug);
        if (slug is null)
            return new ProductLookupResult(false, "That link isn't for this store.");

        var product = await db.Products
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
            .FirstOrDefaultAsync(p => p.StoreId == store.Id && p.Slug == slug && p.IsActive);

        if (product is null)
            return new ProductLookupResult(false, "Product not found.");

        var options = product.Options.ToDictionary(o => o.Name, o => o.Values.Select(v => v.Value).ToList());

        // Only explicit variant overrides show up here (e.g. a size that's out of stock) —
        // combinations with no row here are implicitly available at the base/sale price
        // (smart-defaults model — see the system prompt's lookup_product bullet).
        var variants = product.Variants.Select(v => new ProductVariantInfo(
            v.OptionValues.Count > 0
                ? string.Join(", ", v.OptionValues.Select(ov => $"{ov.OptionValue.ProductOption.Name}: {ov.OptionValue.Value}"))
                : null,
            v.Price,
            v.SalePrice,
            v.Stock,
            v.Stock is null || v.Stock > 0
        )).ToList();

        var description = product.Description is { Length: > MaxDescriptionChars }
            ? product.Description[..MaxDescriptionChars].TrimEnd() + "…"
            : product.Description;

        return new ProductLookupResult(
            true, null,
            product.Name, description, product.Slug,
            product.BasePrice, product.SalePrice,
            options, variants);
    }

    // Reuses CartService's own AddItemAsync/CheckoutAsync rather than a parallel
    // order-creation path, so an AI-drafted order goes through the exact same
    // validation, stock-check, pricing, shipping calc, and confirmation-email logic
    // as a real storefront checkout. Starts with sessionId=null (a fresh cart) on
    // every call — a retried/failed attempt never accumulates leftover items into a
    // later successful one. GetOrCreateCartAsync mints its own SessionId rather than
    // honoring a caller-suggested one, so the real value has to be read back off the
    // first AddItemAsync response and reused for the rest of this call's items + checkout.
    public async Task<DraftOrderResult> DraftOrderAsync(
        Store store,
        Guid conversationId,
        string customerName,
        string phone,
        string address,
        string? email,
        List<DraftOrderItemInput> items,
        string? shippingZoneId,
        string? paymentMethod)
    {
        if (items.Count == 0)
            return new DraftOrderResult(false, "No items to order.");

        string? sessionId = null;

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                return new DraftOrderResult(false, "Quantity must be greater than zero.");

            var product = await db.Products
                .Include(p => p.Options).ThenInclude(o => o.Values)
                .FirstOrDefaultAsync(p => p.StoreId == store.Id && p.Slug == item.ProductSlug && p.IsActive);

            if (product is null)
                return new DraftOrderResult(false, $"Product '{item.ProductSlug}' not found.");

            var configuredOptions = product.Options.Where(o => o.Values.Count > 0).ToList();
            var optionValueIds = new List<Guid>();

            foreach (var option in configuredOptions)
            {
                if (!item.Options.TryGetValue(option.Name, out var desiredValue))
                    return new DraftOrderResult(false, $"Missing a value for option '{option.Name}' on '{product.Name}'.");

                var match = option.Values.FirstOrDefault(v => string.Equals(v.Value, desiredValue, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                    return new DraftOrderResult(false, $"'{desiredValue}' isn't a valid value for '{option.Name}' on '{product.Name}'.");

                optionValueIds.Add(match.Id);
            }

            try
            {
                var cart = await cartService.AddItemAsync(store.Slug, sessionId, new AddCartItemRequest(product.Id, optionValueIds, item.Quantity));
                sessionId = cart.SessionId;
            }
            catch (ArgumentException ex)
            {
                return new DraftOrderResult(false, ex.Message);
            }
        }

        var effectiveEmail = string.IsNullOrWhiteSpace(email) ? $"{conversationId:N}@messenger.nipnip.ge" : email;

        try
        {
            var order = await cartService.CheckoutAsync(
                store.Slug,
                sessionId,
                new CheckoutRequest(customerName, effectiveEmail, phone, address, null, null, paymentMethod ?? "CashOnDelivery", shippingZoneId, null),
                OrderSource.AiAgent);

            // Snapshot the conversation's cumulative usage onto the order it produced, so
            // cost-per-order is queryable without joining back through conversation history.
            // Safe to read straight off the tracked Conversation entity — RecordUsageAsync
            // (called after every Messages.Create this turn, including the one that led to
            // this draft_order call) shares this same scoped AppDbContext.
            var conversationUsage = await db.Conversations.FirstAsync(c => c.Id == conversationId);
            var orderEntity = await db.Orders.FirstAsync(o => o.Id == order.Id);
            orderEntity.AiInputTokens = conversationUsage.TotalInputTokens;
            orderEntity.AiOutputTokens = conversationUsage.TotalOutputTokens;
            orderEntity.AiCacheReadInputTokens = conversationUsage.TotalCacheReadInputTokens;
            orderEntity.AiCacheCreationInputTokens = conversationUsage.TotalCacheCreationInputTokens;
            await db.SaveChangesAsync();

            return new DraftOrderResult(true, null, order.Id, order.Total);
        }
        catch (ArgumentException ex)
        {
            return new DraftOrderResult(false, ex.Message);
        }
    }

    // Structured checkout mechanics from the same ThemeConfig blob CartService itself
    // reads at checkout time (ExtractShipping/ExtractPaymentNotes) — delivery zones/fees
    // plus payment method availability and details (e.g. the actual bank transfer/IBAN
    // notes a customer needs to pay). Not policy prose; that's search_knowledge_base's job.
    public CheckoutInfoResult GetCheckoutInfo(Store store)
    {
        var zones = new List<ShippingZoneInfo>();
        decimal? freeThreshold = null;
        var codEnabled = false;
        string? codNotes = null;
        var bankTransferEnabled = false;
        string? bankTransferNotes = null;

        try
        {
            using var doc = JsonDocument.Parse(store.ThemeConfig);

            if (doc.RootElement.TryGetProperty("shippingZones", out var zonesProp) && zonesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var zone in zonesProp.EnumerateArray())
                {
                    var id = zone.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String
                        ? idProp.GetString()
                        : null;
                    var name = zone.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                        ? nameProp.GetString()
                        : null;
                    var price = zone.TryGetProperty("price", out var priceProp) && priceProp.TryGetDecimal(out var p) ? p : 0m;
                    if (id is not null && name is not null) zones.Add(new ShippingZoneInfo(id, name, price));
                }
            }

            if (doc.RootElement.TryGetProperty("freeShippingThreshold", out var thresholdProp) && thresholdProp.ValueKind == JsonValueKind.Number)
                freeThreshold = thresholdProp.GetDecimal();

            if (doc.RootElement.TryGetProperty("codEnabled", out var codEnabledProp) && codEnabledProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                codEnabled = codEnabledProp.GetBoolean();

            if (doc.RootElement.TryGetProperty("codNotes", out var codNotesProp) && codNotesProp.ValueKind == JsonValueKind.String)
                codNotes = codNotesProp.GetString() is { Length: > 0 } cn ? cn : null;

            if (doc.RootElement.TryGetProperty("bankTransferEnabled", out var btEnabledProp) && btEnabledProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
                bankTransferEnabled = btEnabledProp.GetBoolean();

            if (doc.RootElement.TryGetProperty("bankTransferNotes", out var btNotesProp) && btNotesProp.ValueKind == JsonValueKind.String)
                bankTransferNotes = btNotesProp.GetString() is { Length: > 0 } btn ? btn : null;
        }
        catch (JsonException)
        {
        }

        return new CheckoutInfoResult(zones, freeThreshold, codEnabled, codNotes, bankTransferEnabled, bankTransferNotes);
    }

    // Accepts a bare slug, a path, or a full URL. Full URLs are checked against this
    // store's own subdomain/custom domain and rejected if they point elsewhere.
    private static string? ExtractProductSlug(Store store, string urlOrSlug)
    {
        var trimmed = urlOrSlug.Trim();

        if (!trimmed.Contains('/') && !trimmed.Contains('.'))
            return trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var segments = trimmed.Trim('/').Split('/');
            return segments.Length > 0 && segments[^1].Length > 0 ? segments[^1] : null;
        }

        var expectedHost = $"{store.Slug}.{StorefrontRootDomain}";
        var isOwnStore = string.Equals(uri.Host, expectedHost, StringComparison.OrdinalIgnoreCase)
            || (store.CustomDomain is not null && string.Equals(uri.Host, store.CustomDomain, StringComparison.OrdinalIgnoreCase));

        if (!isOwnStore) return null;

        var pathSegments = uri.AbsolutePath.Trim('/').Split('/');
        return pathSegments.Length > 0 && pathSegments[^1].Length > 0 ? pathSegments[^1] : null;
    }
}
