namespace NipNip.Modules.Storefronts.DTOs;

public record OrderItemResponse(
    Guid Id,
    Guid VariantId,
    Guid ProductId,
    string ProductName,
    string Sku,
    string? ImageUrl,
    List<CartItemOptionResponse> Options,
    int Quantity,
    decimal PriceAtPurchase
);

public record OrderNoteResponse(Guid Id, string Content, DateTimeOffset CreatedAt);

public record OrderBundleItemResponse(Guid Id, Guid BundleId, string BundleName, int Quantity, decimal PriceAtPurchase);

public record OrderDetailResponse(
    Guid Id,
    string CustomerName,
    string Email,
    string Phone,
    string Address,
    double? Latitude,
    double? Longitude,
    string PaymentMethod,
    string Status,
    decimal Total,
    decimal ShippingFee,
    string? ShippingZoneName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaymentConfirmedAt,
    List<OrderItemResponse> Items,
    List<OrderBundleItemResponse> BundleItems,
    List<OrderNoteResponse> Notes,
    int? QuickShipperOrderId,
    string? QuickShipperStatus,
    string? QuickShipperTrackingUrl,
    decimal? QuickShipperDeliveryFee,
    string? CustomerNote,
    string? DiscountCode,
    decimal DiscountAmount,
    bool IsPickup
);

public record UpdateOrderStatusRequest(string Status);

public record CreateOrderNoteRequest(string Content);

public record UpdatePaymentConfirmedRequest(bool Confirmed);

public record NewOrderCountResponse(int Count);

public record MonthlyOrderSummary(
    int Year,
    int Month,
    decimal Revenue,
    int OrderCount,
    int ProductsSold,
    decimal AverageOrderValue,
    decimal AverageItemPrice
);
