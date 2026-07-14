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
        );
}
