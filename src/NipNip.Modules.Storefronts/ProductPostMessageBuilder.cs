using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.Extensions;

namespace NipNip.Modules.Storefronts;

// Shared by Facebook and Instagram publish flows — the merchant always sees this text in
// an editable preview before anything is actually posted, on both platforms.
internal static class ProductPostMessageBuilder
{
    private const string StorefrontRootDomain = "nipnip.ge";

    public static string Build(Product product, Store store)
    {
        var host = store.CustomDomainVerifiedAt.HasValue ? store.CustomDomain! : $"{store.Slug}.{StorefrontRootDomain}";
        var productUrl = $"https://{host}/products/{product.Slug}";

        var price = product.SalePrice ?? product.BasePrice;
        var messageParts = new List<string> { product.DisplayName() };
        var description = product.DisplayDescription();
        if (!string.IsNullOrWhiteSpace(description)) messageParts.Add(description);
        messageParts.Add($"{price} ₾");
        messageParts.Add(productUrl);
        return string.Join("\n\n", messageParts);
    }
}
