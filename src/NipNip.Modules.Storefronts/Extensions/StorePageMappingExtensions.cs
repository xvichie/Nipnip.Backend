using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StorePageMappingExtensions
{
    public static StorePageResponse ToDto(this StorePage page) =>
        new(page.Id, page.Title, page.Slug, page.Content, page.UpdatedAt);
}
