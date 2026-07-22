using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class CategoryMappingExtensions
{
    public static CategoryResponse ToDto(this Category category) =>
        new(category.Id, category.ParentCategoryId, category.Name, category.Slug, category.IconUrl, category.IconKey, category.IconEmoji, category.DefaultOptions);
}
