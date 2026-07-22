namespace NipNip.Modules.Storefronts.DTOs;

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    string Name,
    string Slug,
    string? IconUrl,
    string? IconKey,
    string? IconEmoji,
    string DefaultOptions);

public record CreateCategoryRequest(
    string Name,
    Guid? ParentCategoryId,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null,
    string? DefaultOptions = null);

public record UpdateCategoryRequest(
    string? Name,
    Guid? ParentCategoryId,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null,
    string? DefaultOptions = null);
