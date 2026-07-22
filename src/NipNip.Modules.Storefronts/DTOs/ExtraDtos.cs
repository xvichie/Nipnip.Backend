namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectExtraRequest(string SellerId);

public record ExtraStatusResponse(bool IsConnected, string? SellerId, DateTimeOffset? ConnectedAt);
