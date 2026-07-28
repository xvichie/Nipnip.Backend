namespace NipNip.Modules.Storefronts.DTOs;

public record CategoryResponse(
    Guid Id,
    Guid? ParentCategoryId,
    // Resolved ka -> en -> ru fallback — kept for every existing consumer that just wants
    // "the" name without caring about language (merchant dashboard pickers, CSV export, and
    // every storefront render site not yet updated to pick a name per shopper language).
    string Name,
    string? NameKa,
    string? NameEn,
    string? NameRu,
    string Slug,
    string? IconUrl,
    string? IconKey,
    string? IconEmoji,
    string DefaultOptions);

public record CreateCategoryRequest(
    Guid? ParentCategoryId,
    // At least one of the three must be non-empty — enforced in CategoryService, not here.
    string? NameKa = null,
    string? NameEn = null,
    string? NameRu = null,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null,
    string? DefaultOptions = null);

public record UpdateCategoryRequest(
    Guid? ParentCategoryId,
    string? NameKa = null,
    string? NameEn = null,
    string? NameRu = null,
    string? IconUrl = null,
    string? IconKey = null,
    string? IconEmoji = null,
    string? DefaultOptions = null);
