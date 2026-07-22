namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectCityPayRequest(string CustomerId, string AccessToken);

public record CityPayStatusResponse(bool IsConnected, string? CustomerId, DateTimeOffset? ConnectedAt);
