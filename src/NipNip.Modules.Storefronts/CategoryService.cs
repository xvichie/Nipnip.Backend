using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class CategoryService(AppDbContext db, StoreService storeService)
{
    public async Task<List<CategoryResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var categories = await db.Categories
            .Where(c => c.StoreId == store.Id)
            .ToListAsync();

        return categories.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList();
    }

    public async Task<List<CategoryResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var categories = await db.Categories
            .Where(c => c.StoreId == store.Id)
            .ToListAsync();

        return categories.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(string clerkUserId, CreateCategoryRequest request)
    {
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);

        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        if (request.ParentCategoryId.HasValue &&
            !await db.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value && c.StoreId == store.Id))
            throw new NotFoundException("Parent category not found.");

        ValidateDefaultOptions(request.DefaultOptions);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            ParentCategoryId = request.ParentCategoryId,
            NameKa = nameKa,
            NameEn = nameEn,
            NameRu = nameRu,
            Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!),
            IconUrl = NormalizeIcon(request.IconUrl),
            IconKey = NormalizeIcon(request.IconKey),
            IconEmoji = NormalizeIcon(request.IconEmoji),
            DefaultOptions = request.DefaultOptions ?? "[]",
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category.ToDto();
    }

    private static string? NormalizeIcon(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeName(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // At least one of the three must survive normalization — a category with no name in any
    // language has nothing to display or slugify from.
    private static (string? Ka, string? En, string? Ru) NormalizeNames(string? nameKa, string? nameEn, string? nameRu)
    {
        var (ka, en, ru) = (NormalizeName(nameKa), NormalizeName(nameEn), NormalizeName(nameRu));
        if (ka is null && en is null && ru is null)
            throw new ArgumentException("At least one language name is required.");
        return (ka, en, ru);
    }

    private static void ValidateDefaultOptions(string? defaultOptions)
    {
        if (defaultOptions is null) return;
        try
        {
            JsonDocument.Parse(defaultOptions);
        }
        catch (JsonException)
        {
            throw new ArgumentException("DefaultOptions must be valid JSON.");
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid storeId, string name, Guid? excludeId = null)
    {
        var baseSlug = SlugHelper.Slugify(name, "category");
        var candidate = baseSlug;
        var counter = 2;

        while (await db.Categories.AnyAsync(c => c.StoreId == storeId && c.Slug == candidate && c.Id != excludeId))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }

    public async Task<CategoryResponse> UpdateAsync(string clerkUserId, Guid id, UpdateCategoryRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Category not found.");

        // Frontend always sends all three name fields on every save (never omits them), same
        // convention as the icon fields below — unconditional overwrite, not a HasValue-gated
        // partial update.
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);
        category.NameKa = nameKa;
        category.NameEn = nameEn;
        category.NameRu = nameRu;
        category.Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!, category.Id);

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == id)
                throw new ArgumentException("A category cannot be its own parent.");

            if (!await db.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value && c.StoreId == store.Id))
                throw new NotFoundException("Parent category not found.");
        }

        // The frontend always sends this field on every update (never omits it), so a null here
        // means "clear the parent" — unlike the HasValue-gated fields above, it must always be
        // assigned, not just when non-null, or there'd be no way to remove a category's parent.
        category.ParentCategoryId = request.ParentCategoryId;

        category.IconUrl = NormalizeIcon(request.IconUrl);
        category.IconKey = NormalizeIcon(request.IconKey);
        category.IconEmoji = NormalizeIcon(request.IconEmoji);

        if (request.DefaultOptions is not null)
        {
            ValidateDefaultOptions(request.DefaultOptions);
            category.DefaultOptions = request.DefaultOptions;
        }

        await db.SaveChangesAsync();
        return category.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Category not found.");

        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            throw new ConflictException("Cannot delete a category that still has products assigned to it.");

        if (await db.Categories.AnyAsync(c => c.ParentCategoryId == id))
            throw new ConflictException("Cannot delete a category that still has subcategories.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }

    // --- Admin-scoped (building out a prospect's demo store) — resolves by merchantId instead
    // of the caller's own Clerk identity. See ProductService's admin methods for why these
    // aren't merged into the self-service ones above. ---

    public async Task<List<CategoryResponse>> GetAllAdminForMerchantAsync(Guid merchantId)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var categories = await db.Categories
            .Where(c => c.StoreId == store.Id)
            .ToListAsync();

        return categories.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList();
    }

    public async Task<CategoryResponse> CreateAdminAsync(Guid merchantId, CreateCategoryRequest request)
    {
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);

        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        if (request.ParentCategoryId.HasValue &&
            !await db.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value && c.StoreId == store.Id))
            throw new NotFoundException("Parent category not found.");

        ValidateDefaultOptions(request.DefaultOptions);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            ParentCategoryId = request.ParentCategoryId,
            NameKa = nameKa,
            NameEn = nameEn,
            NameRu = nameRu,
            Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!),
            IconUrl = NormalizeIcon(request.IconUrl),
            IconKey = NormalizeIcon(request.IconKey),
            IconEmoji = NormalizeIcon(request.IconEmoji),
            DefaultOptions = request.DefaultOptions ?? "[]",
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category.ToDto();
    }

    public async Task<CategoryResponse> UpdateAdminAsync(Guid merchantId, Guid id, UpdateCategoryRequest request)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Category not found.");

        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);
        category.NameKa = nameKa;
        category.NameEn = nameEn;
        category.NameRu = nameRu;
        category.Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!, category.Id);

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == id)
                throw new ArgumentException("A category cannot be its own parent.");

            if (!await db.Categories.AnyAsync(c => c.Id == request.ParentCategoryId.Value && c.StoreId == store.Id))
                throw new NotFoundException("Parent category not found.");
        }

        category.ParentCategoryId = request.ParentCategoryId;
        category.IconUrl = NormalizeIcon(request.IconUrl);
        category.IconKey = NormalizeIcon(request.IconKey);
        category.IconEmoji = NormalizeIcon(request.IconEmoji);

        if (request.DefaultOptions is not null)
        {
            ValidateDefaultOptions(request.DefaultOptions);
            category.DefaultOptions = request.DefaultOptions;
        }

        await db.SaveChangesAsync();
        return category.ToDto();
    }

    public async Task DeleteAdminAsync(Guid merchantId, Guid id)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Category not found.");

        if (await db.Products.AnyAsync(p => p.CategoryId == id))
            throw new ConflictException("Cannot delete a category that still has products assigned to it.");

        if (await db.Categories.AnyAsync(c => c.ParentCategoryId == id))
            throw new ConflictException("Cannot delete a category that still has subcategories.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}
