using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductBundleMappingExtensions
{
    public static ProductBundleResponse ToDto(this ProductBundle bundle)
    {
        var items = bundle.Items.Select(i => new BundleItemResponse(
            i.ProductId,
            i.Product.Name,
            i.Product.Slug,
            i.Product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url,
            i.Product.SalePrice ?? i.Product.BasePrice,
            i.Quantity
        )).ToList();

        var regularTotal = items.Sum(i => i.ProductPrice * i.Quantity);

        return new ProductBundleResponse(
            bundle.Id,
            bundle.Name,
            bundle.Slug,
            bundle.BundlePrice,
            bundle.ImageUrl,
            bundle.IsActive,
            regularTotal,
            items
        );
    }
}
