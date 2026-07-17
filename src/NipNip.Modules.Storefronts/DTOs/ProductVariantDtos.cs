namespace NipNip.Modules.Storefronts.DTOs;

public record ProductVariantResponse(Guid Id, string Sku, decimal Price, decimal? SalePrice, int? Stock, List<Guid> OptionValueIds);

/// <summary>Stock null means unlimited.</summary>
public record CreateProductVariantRequest(string Sku, decimal Price, decimal? SalePrice, int? Stock, List<Guid> OptionValueIds);

/// <summary>
/// Stock is only applied when a value is provided. To explicitly clear an existing stock
/// number back to unlimited, set ClearStock true (Stock itself can't carry that signal,
/// since "omitted" and "sent as null" are indistinguishable once deserialized).
/// </summary>
public record UpdateProductVariantRequest(string? Sku, decimal? Price, decimal? SalePrice, int? Stock, bool ClearStock = false);
