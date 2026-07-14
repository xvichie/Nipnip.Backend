namespace NipNip.Modules.Storefronts.DTOs;

public record ProductVariantResponse(Guid Id, string Sku, decimal Price, decimal? SalePrice, int Stock, List<Guid> OptionValueIds);

public record CreateProductVariantRequest(string Sku, decimal Price, decimal? SalePrice, int Stock, List<Guid> OptionValueIds);

public record UpdateProductVariantRequest(string? Sku, decimal? Price, decimal? SalePrice, int? Stock);
