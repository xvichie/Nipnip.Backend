namespace NipNip.Modules.Storefronts.DTOs;

public record CheckoutRequest(
    string CustomerName,
    string Email,
    string Phone,
    string Address,
    double? Latitude,
    double? Longitude,
    string PaymentMethod,
    string? ShippingZoneId,
    string? Ref
);

public record OrderResponse(
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
    DateTimeOffset CreatedAt
);
