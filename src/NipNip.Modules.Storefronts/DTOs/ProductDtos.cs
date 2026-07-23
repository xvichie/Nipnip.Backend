namespace NipNip.Modules.Storefronts.DTOs;

public record ProductSummaryResponse(
    Guid Id,
    string Slug,
    Guid? CategoryId,
    string Name,
    decimal BasePrice,
    decimal? SalePrice,
    bool IsActive,
    string? ThumbnailUrl,
    string? SecondImageUrl,
    DateTimeOffset CreatedAt
);

public record ProductDetailResponse(
    Guid Id,
    string Slug,
    Guid? CategoryId,
    string Name,
    string? Description,
    string? VideoUrl,
    decimal BasePrice,
    decimal? SalePrice,
    bool IsActive,
    List<ProductImageResponse> Images,
    List<ProductOptionResponse> Options,
    List<ProductVariantResponse> Variants,
    List<ProductSummaryResponse> RelatedProducts
);

public record CreateProductRequest(string Name, string? Description, string? VideoUrl, decimal BasePrice, decimal? SalePrice, Guid? CategoryId);

/// <summary>
/// VideoUrl: send "" to clear an existing video back to none, a URL to set/replace it,
/// or omit the field to leave it untouched (empty string and omission are distinguishable
/// once deserialized — null is not, since a nullable string omits and nulls identically).
/// </summary>
public record UpdateProductRequest(string? Name, string? Description, string? VideoUrl, decimal? BasePrice, decimal? SalePrice, Guid? CategoryId, bool? IsActive);

public record ProductPriceRangeResponse(decimal Min, decimal Max);

/// <summary>Distinct option name + the values it takes across a store's (optionally category-scoped) active products.</summary>
public record ProductFacetResponse(string Name, List<string> Values);

/// <summary>One group of a listing-page option filter — e.g. Name="ზომა", Values=["35","36"]. Selected values within
/// a group are OR'd together; separate groups (passed as a JSON array) are AND'd together by the caller.</summary>
public record OptionFilterInput(string Name, List<string> Values);
