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
    int Stock,
    List<CartItemOptionResponse> Options
);

public record CartResponse(Guid Id, string SessionId, List<CartItemResponse> Items, decimal Total);

public record AddCartItemRequest(Guid VariantId, int Quantity);

public record UpdateCartItemRequest(int Quantity);
