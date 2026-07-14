namespace NipNip.Modules.Storefronts.DTOs;

public record ContactMessageResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAt
);

public record CreateContactMessageRequest(string Name, string? Email, string? Phone, string Message);

public record UnreadContactMessageCountResponse(int Count);
