namespace NipNip.Modules.Storefronts.DTOs;

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    string? IconUrl,
    string? IconKey,
    string? IconEmoji);

public record CreateCategoryRequest(
    string Name,
    Guid? ParentCategoryId,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null);

public record UpdateCategoryRequest(
    string? Name,
    Guid? ParentCategoryId,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null);
