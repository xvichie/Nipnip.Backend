using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductVariantMappingExtensions
{
    public static ProductVariantResponse ToDto(this ProductVariant variant) =>
        new(
            variant.Id,
            variant.Sku,
            variant.Price,
            variant.SalePrice,
            variant.Stock,
            variant.OptionValues.Select(ov => ov.OptionValueId).ToList()
        );
}
