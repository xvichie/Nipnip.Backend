namespace NipNip.Modules.Storefronts.DTOs;

public record BundleItemResponse(
    Guid ProductId,
    string ProductName, // resolved ka -> en -> ru fallback
    string? ProductNameKa,
    string? ProductNameEn,
    string? ProductNameRu,
    string ProductSlug,
    string? ImageUrl,
    decimal ProductPrice,
    int Quantity
);

public record ProductBundleResponse(
    Guid Id,
    // Resolved ka -> en -> ru fallback — for consumers that just want "the" name.
    string Name,
    string? NameKa,
    string? NameEn,
    string? NameRu,
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
    // At least one of the three must be non-empty — enforced in ProductBundleService, not here.
    string? NameKa,
    string? NameEn,
    string? NameRu,
    decimal BundlePrice,
    string? ImageUrl,
    List<BundleItemInput> Items
);

public record UpdateProductBundleRequest(
    string? NameKa,
    string? NameEn,
    string? NameRu,
    decimal? BundlePrice,
    string? ImageUrl,
    bool? IsActive,
    List<BundleItemInput>? Items
);
