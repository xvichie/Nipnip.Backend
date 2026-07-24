namespace NipNip.Modules.Storefronts.DTOs;

public record StoreDiscountCodeResponse(
    Guid Id,
    string Code,
    string Type,
    decimal Value,
    decimal? MinOrderAmount,
    int? MaxUses,
    int UsesCount,
    DateTimeOffset? ExpiresAt,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record CreateStoreDiscountCodeRequest(
    string Code,
    string Type,
    decimal Value,
    decimal? MinOrderAmount,
    int? MaxUses,
    DateTimeOffset? ExpiresAt
);

public record UpdateStoreDiscountCodeRequest(
    string Code,
    string Type,
    decimal Value,
    decimal? MinOrderAmount,
    int? MaxUses,
    DateTimeOffset? ExpiresAt,
    bool IsActive
);

public record ValidateDiscountCodeRequest(string Code, decimal Subtotal);

public record ValidateDiscountCodeResponse(
    bool Valid,
    decimal DiscountAmount,
    string? ErrorCode,
    decimal? MinOrderAmount
);
