using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class CartMappingExtensions
{
    public static CartResponse ToDto(this Cart cart) =>
        new(
            cart.Id,
            cart.SessionId,
            cart.Items.Select(i => new CartItemResponse(
                i.Id,
                i.VariantId,
                i.Variant.Product.Name,
                i.Variant.Product.Slug,
                i.Variant.Sku,
                i.Variant.SalePrice ?? i.Variant.Price,
                i.Quantity,
                i.Variant.Product.Images.OrderBy(img => img.SortOrder).FirstOrDefault()?.Url,
                i.Variant.Stock,
                i.Variant.OptionValues
                    .Select(ov => new CartItemOptionResponse(ov.OptionValue.ProductOption.Name, ov.OptionValue.Value))
                    .ToList()
            )).ToList(),
            cart.Items.Sum(i => (i.Variant.SalePrice ?? i.Variant.Price) * i.Quantity)
                + cart.BundleItems.Sum(i => i.Bundle.BundlePrice * i.Quantity),
            cart.BundleItems.Select(i => new CartBundleItemResponse(
                i.Id,
                i.BundleId,
                i.Bundle.Name,
                i.Bundle.Slug,
                i.Bundle.ImageUrl,
                i.Bundle.BundlePrice,
                i.Quantity
            )).ToList()
        );
}
