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
    string? SecondImageUrl
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
