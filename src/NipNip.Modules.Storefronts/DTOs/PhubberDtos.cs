namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectPhubberRequest(string SellerId);

public record PhubberStatusResponse(bool IsConnected, string? SellerId, DateTimeOffset? ConnectedAt);
