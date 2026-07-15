namespace NipNip.Modules.Storefronts.DTOs;

public record StoreResponse(
    Guid Id,
    string Slug,
    string Name,
    string ThemeId,
    string ThemeConfig,
    bool IsActive,
    bool AffiliateEnabled,
    DateTimeOffset CreatedAt
);

public record CreateStoreRequest(string Slug, string Name, string? ThemeId, string? ThemeConfig);

public record UpdateStoreRequest(string? Name, string? ThemeId, string? ThemeConfig, bool? IsActive, bool? AffiliateEnabled);

public record SetStoreDomainRequest(string Domain);

public record DomainDnsRecordResponse(string Type, string Name, string Value);

public record StoreDomainResponse(
    string? Domain,
    bool Verified,
    DateTimeOffset? VerifiedAt,
    List<DomainDnsRecordResponse> Instructions
);

public record StoreSlugResponse(string Slug);
