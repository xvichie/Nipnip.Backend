using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductMappingExtensions
{
    public static ProductSummaryResponse ToSummaryDto(this Product product)
    {
        var orderedImageUrls = product.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).ToList();

        return new(
            product.Id,
            product.Slug,
            product.CategoryId,
            product.Name,
            product.BasePrice,
            product.SalePrice,
            product.IsActive,
            orderedImageUrls.FirstOrDefault(),
            orderedImageUrls.Skip(1).FirstOrDefault()
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
            product.Name,
            product.Description,
            product.VideoUrl,
            product.BasePrice,
            product.SalePrice,
            product.IsActive,
            product.Images.OrderBy(i => i.SortOrder).Select(i => i.ToDto()).ToList(),
            product.Options.Select(o => o.ToDto()).ToList(),
            product.Variants.Select(v => v.ToDto()).ToList(),
            relatedProducts ?? []
        );
}
