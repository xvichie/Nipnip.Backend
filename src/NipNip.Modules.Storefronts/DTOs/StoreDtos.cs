namespace NipNip.Modules.Storefronts.DTOs;

public record StoreResponse(
    Guid Id,
    string Slug,
    string Name,
    string ThemeId,
    string ThemeConfig,
    bool IsActive,
    bool AffiliateEnabled,
    DateTimeOffset CreatedAt,
    string? CustomDomain,
    string? ThemeOverride,
    bool ThemeOverrideEnabled,
    bool IsProspect
);

public record CreateStoreRequest(string Slug, string Name, string? ThemeId, string? ThemeConfig);

public record UpdateStoreRequest(string? Name, string? ThemeId, string? ThemeConfig, bool? IsActive, bool? AffiliateEnabled, bool? ThemeOverrideEnabled);

// Admin-only — sets the ThemeOverride *content*. Pass null to clear it.
public record SetStoreThemeOverrideRequest(string? ThemeOverride);

public record SetStoreDomainRequest(string Domain);

public record DomainDnsRecordResponse(string Type, string Name, string Value);

public record StoreDomainResponse(
    string? Domain,
    bool Verified,
    DateTimeOffset? VerifiedAt,
    List<DomainDnsRecordResponse> Instructions
);

public record StoreSlugResponse(string Slug);

public record StoreSitemapEntryResponse(string Slug, string? CustomDomain);
