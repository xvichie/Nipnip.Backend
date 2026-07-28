using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductBundleMappingExtensions
{
    public static string DisplayName(this ProductBundle bundle) => bundle.NameKa ?? bundle.NameEn ?? bundle.NameRu ?? "";

    // Same ka-priority fallback as DisplayName(), but lets "en"/"ru" pick their own translation
    // first (when set) — mirrors the frontend's getBundleName(bundle, lang).
    public static string DisplayName(this ProductBundle bundle, string lang) => lang switch
    {
        "en" when !string.IsNullOrEmpty(bundle.NameEn) => bundle.NameEn,
        "ru" when !string.IsNullOrEmpty(bundle.NameRu) => bundle.NameRu,
        _ => bundle.DisplayName(),
    };

    public static ProductBundleResponse ToDto(this ProductBundle bundle)
    {
        var items = bundle.Items.Select(i => new BundleItemResponse(
            i.ProductId,
            i.Product.DisplayName(),
            i.Product.NameKa,
            i.Product.NameEn,
            i.Product.NameRu,
            i.Product.Slug,
            i.Product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url,
            i.Product.SalePrice ?? i.Product.BasePrice,
            i.Quantity
        )).ToList();

        var regularTotal = items.Sum(i => i.ProductPrice * i.Quantity);

        return new ProductBundleResponse(
            bundle.Id,
            bundle.DisplayName(),
            bundle.NameKa,
            bundle.NameEn,
            bundle.NameRu,
            bundle.Slug,
            bundle.BundlePrice,
            bundle.ImageUrl,
            bundle.IsActive,
            regularTotal,
            items
        );
    }
}
