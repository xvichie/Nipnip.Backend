namespace NipNip.Modules.Storefronts.DTOs;

public record ProductSummaryResponse(
    Guid Id,
    string Slug,
    Guid? CategoryId,
    string Name, // resolved ka->en->ru fallback
    string? NameKa,
    string? NameEn,
    string? NameRu,
    decimal BasePrice,
    decimal? SalePrice,
    bool IsActive,
    string? ThumbnailUrl,
    string? SecondImageUrl,
    DateTimeOffset CreatedAt,
    List<Guid> CollectionIds
);

public record ProductDetailResponse(
    Guid Id,
    string Slug,
    Guid? CategoryId,
    string Name, // resolved ka->en->ru fallback
    string? NameKa,
    string? NameEn,
    string? NameRu,
    string? Description, // resolved ka->en->ru fallback
    string? DescriptionKa,
    string? DescriptionEn,
    string? DescriptionRu,
    string? VideoUrl,
    decimal BasePrice,
    decimal? SalePrice,
    bool IsActive,
    List<ProductImageResponse> Images,
    List<ProductOptionResponse> Options,
    List<ProductVariantResponse> Variants,
    List<ProductSummaryResponse> RelatedProducts,
    List<Guid> CollectionIds
);

/// <summary>
/// At least one of NameKa/NameEn/NameRu is required (validated in ProductService), same rule
/// as categories. Description fields are all optional. CollectionIds: new memberships are
/// appended to the end of each target collection's order.
/// </summary>
public record CreateProductRequest(
    string? NameKa,
    string? NameEn,
    string? NameRu,
    string? DescriptionKa,
    string? DescriptionEn,
    string? DescriptionRu,
    string? VideoUrl,
    decimal BasePrice,
    decimal? SalePrice,
    Guid? CategoryId,
    List<Guid>? CollectionIds = null
);

/// <summary>
/// NameKa/NameEn/NameRu and DescriptionKa/DescriptionEn/DescriptionRu are always sent together
/// and unconditionally overwrite the existing values (same convention as categories' name/icon
/// fields) — the frontend always resends the complete current state of all six on every save.
/// VideoUrl: send "" to clear an existing video back to none, a URL to set/replace it,
/// or omit the field to leave it untouched (empty string and omission are distinguishable
/// once deserialized — null is not, since a nullable string omits and nulls identically).
/// CollectionIds: omit to leave membership unchanged; pass a list (possibly empty) to replace
/// it outright — new memberships are appended to the end of each target collection's order.
/// </summary>
public record UpdateProductRequest(
    string? NameKa,
    string? NameEn,
    string? NameRu,
    string? DescriptionKa,
    string? DescriptionEn,
    string? DescriptionRu,
    string? VideoUrl,
    decimal? BasePrice,
    decimal? SalePrice,
    Guid? CategoryId,
    bool? IsActive,
    List<Guid>? CollectionIds = null
);

public record ProductPriceRangeResponse(decimal Min, decimal Max);

/// <summary>One filterable value within a facet group — Value is the canonical string used in
/// filter query params and matched against ProductOptionValue.Value; the 3 translation fields
/// are display-only overlays.</summary>
public record ProductFacetValueResponse(string Value, string? ValueKa, string? ValueEn, string? ValueRu);

/// <summary>Distinct option name + the values it takes across a store's (optionally category-scoped) active products.
/// Name is the resolved ka->en->ru fallback, grouped across every ProductOption row sharing that canonical name.</summary>
public record ProductFacetResponse(string Name, string? NameKa, string? NameEn, string? NameRu, List<ProductFacetValueResponse> Values);

/// <summary>One group of a listing-page option filter — e.g. Name="ზომა", Values=["35","36"]. Selected values within
/// a group are OR'd together; separate groups (passed as a JSON array) are AND'd together by the caller.</summary>
public record OptionFilterInput(string Name, List<string> Values);
