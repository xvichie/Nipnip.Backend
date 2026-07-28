namespace NipNip.Modules.Storefronts.DTOs;

public record StorePageResponse(
    Guid Id,
    string Title, // resolved ka->en->ru fallback
    string? TitleKa,
    string? TitleEn,
    string? TitleRu,
    string Slug,
    string Content, // resolved ka->en->ru fallback
    string? ContentKa,
    string? ContentEn,
    string? ContentRu,
    DateTimeOffset UpdatedAt
);

/// <summary>At least one of TitleKa/TitleEn/TitleRu and one of ContentKa/ContentEn/ContentRu are required.</summary>
public record CreateStorePageRequest(
    string? TitleKa,
    string? TitleEn,
    string? TitleRu,
    string? ContentKa,
    string? ContentEn,
    string? ContentRu
);

/// <summary>Unconditionally overwrites all title/content fields, same convention as categories/products.</summary>
public record UpdateStorePageRequest(
    string? TitleKa,
    string? TitleEn,
    string? TitleRu,
    string? ContentKa,
    string? ContentEn,
    string? ContentRu
);
