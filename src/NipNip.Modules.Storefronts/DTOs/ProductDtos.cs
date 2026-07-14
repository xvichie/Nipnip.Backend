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
    decimal BasePrice,
    decimal? SalePrice,
    bool IsActive,
    List<ProductImageResponse> Images,
    List<ProductOptionResponse> Options,
    List<ProductVariantResponse> Variants
);

public record CreateProductRequest(string Name, string? Description, decimal BasePrice, decimal? SalePrice, Guid? CategoryId);

public record UpdateProductRequest(string? Name, string? Description, decimal? BasePrice, decimal? SalePrice, Guid? CategoryId, bool? IsActive);
