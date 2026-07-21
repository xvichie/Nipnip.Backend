namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectMyMarketRequest(string ShopId);

public record MyMarketStatusResponse(bool IsConnected, string? ShopId, DateTimeOffset? ConnectedAt);
