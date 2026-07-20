namespace NipNip.Modules.Storefronts.DTOs;

public record ConnectFlittRequest(string MerchantId, string SecretKey);

public record FlittStatusResponse(bool IsConnected, string? MerchantId, DateTimeOffset? ConnectedAt);
