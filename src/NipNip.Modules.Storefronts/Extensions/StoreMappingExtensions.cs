using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StoreMappingExtensions
{
    public static StoreResponse ToDto(this Store store) =>
        new(store.Id, store.Slug, store.Name, store.ThemeId, store.ThemeConfig, store.IsActive, store.AffiliateEnabled, store.CreatedAt);
}
