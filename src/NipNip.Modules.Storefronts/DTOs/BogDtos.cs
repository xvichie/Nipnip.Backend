namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectBogRequest(string ClientId, string ClientSecret);

public record BogStatusResponse(bool IsConnected, string? ClientId, DateTimeOffset? ConnectedAt);
