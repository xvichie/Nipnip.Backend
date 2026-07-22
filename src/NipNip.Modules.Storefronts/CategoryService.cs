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
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<CategoryResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var categories = await db.Categories
            .Where(c => c.StoreId == store.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoryResponse> CreateAsync(string clerkUserId, CreateCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

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
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(store.Id, request.Name),
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

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            category.Name = request.Name.Trim();
            category.Slug = await GenerateUniqueSlugAsync(store.Id, category.Name, category.Id);
        }

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
}
