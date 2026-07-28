namespace NipNip.Modules.Storefronts.DTOs;

public record CollectionResponse(
    Guid Id,
    // Resolved ka -> en -> ru fallback — kept for every existing consumer that just wants
    // "the" name without caring about language.
    string Name,
    string? NameKa,
    string? NameEn,
    string? NameRu,
    string Slug);

public record CreateCollectionRequest(
    // At least one of the three must be non-empty — enforced in CollectionService, not here.
    string? NameKa = null,
    string? NameEn = null,
    string? NameRu = null);

public record UpdateCollectionRequest(
    string? NameKa = null,
    string? NameEn = null,
    string? NameRu = null);

/// <summary>Full ordered replace of a collection's product membership — mirrors SetRelatedProductsRequest.</summary>
public record SetCollectionProductsRequest(List<Guid> ProductIds);
