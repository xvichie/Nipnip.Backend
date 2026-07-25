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
    // Optional — the onboarding-replacement flow (see app/onboarding/page.tsx) collects only
    // name/contact/store name, no message. The marketing-site footer form still asks for one.
    string? Message = null
);

public record UnreadWebsiteInquiryCountResponse(int Count);
