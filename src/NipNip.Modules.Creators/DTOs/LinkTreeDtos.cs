namespace NipNip.Modules.Creators.DTOs;

public record LinkTreeItemResponse(
    Guid Id,
    Guid MerchantId,
    string MerchantName,
    string MerchantSlug,
    string? MerchantLogoUrl,
    string? Label,
    int Position
);

public record LinkTreeSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsDefault,
    int Position,
    int ItemCount
);

public record LinkTreeDetailResponse(
    Guid Id,
    string Name,
    string Slug,
    bool IsDefault,
    IReadOnlyList<LinkTreeItemResponse> Items
);

public record PublicLinkTreeResponse(
    string CreatorName,
    string CreatorSlug,
    string? CreatorAvatarUrl,
    string TreeName,
    IReadOnlyList<LinkTreeItemResponse> Items
);

public record CreateLinkTreeRequest(string Name, string Slug);

public record UpdateLinkTreeRequest(string? Name, string? Slug);

public record AddLinkTreeItemRequest(Guid MerchantId, string? Label);

public record UpdateLinkTreeItemRequest(string? Label);

public record ReorderLinkTreeItemsRequest(List<Guid> OrderedItemIds);
