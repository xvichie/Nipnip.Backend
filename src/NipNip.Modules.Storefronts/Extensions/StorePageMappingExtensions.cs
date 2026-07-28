using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StorePageMappingExtensions
{
    public static string DisplayTitle(this StorePage page) => page.TitleKa ?? page.TitleEn ?? page.TitleRu ?? "";

    public static string DisplayContent(this StorePage page) => page.ContentKa ?? page.ContentEn ?? page.ContentRu ?? "";

    public static StorePageResponse ToDto(this StorePage page) =>
        new(
            page.Id,
            page.DisplayTitle(),
            page.TitleKa,
            page.TitleEn,
            page.TitleRu,
            page.Slug,
            page.DisplayContent(),
            page.ContentKa,
            page.ContentEn,
            page.ContentRu,
            page.UpdatedAt
        );
}
