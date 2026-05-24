namespace NipNip.Modules.Codes.DTOs;

public record CodeResponse(
    Guid Id,
    Guid CreatorId,
    Guid MerchantId,
    string Code,
    DateTimeOffset CreatedAt);
