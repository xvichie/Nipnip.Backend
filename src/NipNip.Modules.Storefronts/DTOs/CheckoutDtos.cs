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
    string? Ref,
    string? CustomerNote = null,
    string? DiscountCode = null,
    bool IsPickup = false,
    /// <summary>Shopper's checkout-time language ("ka"/"en"/"ru"); resolves codNotes/bankTransferNotes/shippingZone name and freezes onto Order.ShippingZoneName. Defaults to "ka".</summary>
    string? Lang = null
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
    DateTimeOffset CreatedAt,
    string? RedirectUrl = null,
    string? CustomerNote = null,
    string? DiscountCode = null,
    decimal DiscountAmount = 0m,
    bool IsPickup = false
);
