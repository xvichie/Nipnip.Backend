using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductMappingExtensions
{
    public static string DisplayName(this Product product) => product.NameKa ?? product.NameEn ?? product.NameRu ?? "";

    // Same ka-priority fallback as DisplayName(), but lets "en"/"ru" pick their own translation
    // first (when set) — mirrors the frontend's getProductName(product, lang).
    public static string DisplayName(this Product product, string lang) => lang switch
    {
        "en" when !string.IsNullOrEmpty(product.NameEn) => product.NameEn,
        "ru" when !string.IsNullOrEmpty(product.NameRu) => product.NameRu,
        _ => product.DisplayName(),
    };

    public static string? DisplayDescription(this Product product) => product.DescriptionKa ?? product.DescriptionEn ?? product.DescriptionRu;

    public static ProductSummaryResponse ToSummaryDto(this Product product)
    {
        var orderedImageUrls = product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList();

        return new(
            product.Id,
            product.Slug,
            product.CategoryId,
            product.DisplayName(),
            product.NameKa,
            product.NameEn,
            product.NameRu,
            product.BasePrice,
            product.SalePrice,
            product.IsActive,
            orderedImageUrls.FirstOrDefault(),
            orderedImageUrls.Skip(1).FirstOrDefault(),
            product.CreatedAt,
            product.ProductCollections.Select(pc => pc.CollectionId).ToList()
        );
    }

    // relatedProducts is only ever populated by the public storefront single-product
    // fetch — it needs a separate cross-product query, so callers that don't already
    // have that (merchant CRUD, duplication, etc.) simply omit it.
    public static ProductDetailResponse ToDetailDto(this Product product, List<ProductSummaryResponse>? relatedProducts = null) =>
        new(
            product.Id,
            product.Slug,
            product.CategoryId,
            product.DisplayName(),
            product.NameKa,
            product.NameEn,
            product.NameRu,
            product.DisplayDescription(),
            product.DescriptionKa,
            product.DescriptionEn,
            product.DescriptionRu,
            product.VideoUrl,
            product.BasePrice,
            product.SalePrice,
            product.IsActive,
            product.Images.OrderBy(i => i.SortOrder).Select(i => i.ToDto()).ToList(),
            product.Options.Select(o => o.ToDto()).ToList(),
            product.Variants.Select(v => v.ToDto()).ToList(),
            relatedProducts ?? [],
            product.ProductCollections.Select(pc => pc.CollectionId).ToList()
        );
}
