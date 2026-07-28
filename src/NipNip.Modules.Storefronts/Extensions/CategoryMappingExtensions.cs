using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class CategoryMappingExtensions
{
    // ka -> en -> ru fallback — every existing consumer that just wants "the" name (dashboard
    // pickers, CSV export/import, un-migrated storefront render sites) reads this instead of a
    // specific language.
    public static string DisplayName(this Category category) => category.NameKa ?? category.NameEn ?? category.NameRu ?? "";

    public static CategoryResponse ToDto(this Category category) =>
        new(category.Id, category.ParentCategoryId, category.DisplayName(), category.NameKa, category.NameEn, category.NameRu,
            category.Slug, category.IconUrl, category.IconKey, category.IconEmoji, category.DefaultOptions);
}
