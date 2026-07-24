namespace NipNip.Modules.Merchants.DTOs;

public record WebsiteInquiryResponse(
    Guid Id,
    string Name,
    string? StoreName,
    string? Email,
    string? Phone,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt
);

public record CreateWebsiteInquiryRequest(
    string Name,
    string? StoreName,
    string? Email,
    string? Phone,
    string Message
);

public record UnreadWebsiteInquiryCountResponse(int Count);
