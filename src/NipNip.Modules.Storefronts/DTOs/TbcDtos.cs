namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectTbcRequest(string ClientId, string ClientSecret);

public record TbcStatusResponse(bool IsConnected, string? ClientId, DateTimeOffset? ConnectedAt);
