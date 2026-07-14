namespace NipNip.Modules.Storefronts.DTOs;

public record StorePageResponse(Guid Id, string Title, string Slug, string Content, DateTimeOffset UpdatedAt);

public record CreateStorePageRequest(string Title, string Content);

public record UpdateStorePageRequest(string? Title, string? Content);
