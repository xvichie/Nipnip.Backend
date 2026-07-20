namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectQuickShipperRequest(string Username, string Password);

public record QuickShipperStatusResponse(bool IsConnected, DateTimeOffset? ConnectedAt, bool HasPickupLocation);

public record SavePickupLocationRequest(string Address, double Latitude, double Longitude, string ContactName, string Phone);

public record PickupLocationResponse(string? Address, double? Latitude, double? Longitude, string? ContactName, string? Phone);

public record QuickShipperCustomFieldResponse(
    int Id,
    string? Name,
    bool IsOptional,
    string? Description,
    string? Placeholder,
    List<string>? ListValues,
    string? Type,
    string? ValueType
);

public record QuickShipperFeeOptionResponse(
    int ProviderId,
    string? ProviderName,
    string? ProviderLogoUrl,
    decimal Price,
    string? Currency,
    string? DeliverySpeedName,
    string? PriceId,
    bool HasCashOnDelivery,
    bool IsActive
);

public record QuickShipperFeesResponse(List<QuickShipperFeeOptionResponse> Options, double Distance);

public record QuickShipperCustomFieldValueRequest(int Id, string Value, string? Type);

public record CreateQuickShipperOrderRequest(int ProviderId, string? PriceId, List<QuickShipperCustomFieldValueRequest>? CustomFieldValues);
