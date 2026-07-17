namespace NipNip.Modules.Storefronts.DTOs;

public record CartItemOptionResponse(string OptionName, string Value);

public record CartItemResponse(
    Guid Id,
    Guid VariantId,
    string ProductName,
    string ProductSlug,
    string Sku,
    decimal Price,
    int Quantity,
    string? ImageUrl,
    int? Stock,
    List<CartItemOptionResponse> Options
);

public record CartResponse(Guid Id, string SessionId, List<CartItemResponse> Items, decimal Total);

/// <summary>
/// OptionValueIds must cover exactly one value per configured product option (empty for
/// products with no options). The backend resolves the matching variant, or materializes
/// a default one (base price, unlimited stock) the first time that combination is bought.
/// </summary>
public record AddCartItemRequest(Guid ProductId, List<Guid> OptionValueIds, int Quantity);

public record UpdateCartItemRequest(int Quantity);
