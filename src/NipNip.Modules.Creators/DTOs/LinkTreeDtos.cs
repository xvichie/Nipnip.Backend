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

public record LinkTreeResponse(
    string CreatorName,
    string CreatorSlug,
    string? CreatorAvatarUrl,
    IReadOnlyList<LinkTreeItemResponse> Items
);

public record AddLinkTreeItemRequest(Guid MerchantId, string? Label);

public record UpdateLinkTreeItemRequest(string? Label);

public record ReorderLinkTreeItemsRequest(List<Guid> OrderedItemIds);
