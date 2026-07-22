using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts.Extensions;

public static class StoreMappingExtensions
{
    public static StoreResponse ToDto(this Store store) =>
        // Only surfaced once actually verified — an unverified custom domain may not resolve or
        // even belong to the merchant yet, so it must never be used as a canonical/public URL.
        new(store.Id, store.Slug, store.Name, store.ThemeId, store.ThemeConfig, store.IsActive, store.AffiliateEnabled, store.CreatedAt,
            store.CustomDomainVerifiedAt.HasValue ? store.CustomDomain : null,
            store.ThemeOverride, store.ThemeOverrideEnabled);
}
