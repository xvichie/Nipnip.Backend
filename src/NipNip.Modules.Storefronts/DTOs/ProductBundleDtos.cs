namespace NipNip.Modules.Storefronts.DTOs;

public record BundleItemResponse(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string? ImageUrl,
    decimal ProductPrice,
    int Quantity
);

public record ProductBundleResponse(
    Guid Id,
    string Name,
    string Slug,
    decimal BundlePrice,
    string? ImageUrl,
    bool IsActive,
    /// <summary>Sum of each item's current effective price × quantity — lets the UI show "you save ₾X".</summary>
    decimal RegularTotal,
    List<BundleItemResponse> Items
);

public record BundleItemInput(Guid ProductId, int Quantity);

public record CreateProductBundleRequest(
    string Name,
    decimal BundlePrice,
    string? ImageUrl,
    List<BundleItemInput> Items
);

public record UpdateProductBundleRequest(
    string? Name,
    decimal? BundlePrice,
    string? ImageUrl,
    bool? IsActive,
    List<BundleItemInput>? Items
);
