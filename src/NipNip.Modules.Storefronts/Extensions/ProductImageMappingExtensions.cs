using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class ProductImageMappingExtensions
{
    public static ProductImageResponse ToDto(this ProductImage image) =>
        new(image.Id, image.Url, image.SortOrder);
}
