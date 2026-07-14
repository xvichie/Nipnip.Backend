using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class StorePageService(AppDbContext db, StoreService storeService)
{
    public async Task<List<StorePageResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var pages = await db.StorePages
            .Where(p => p.StoreId == store.Id)
            .OrderBy(p => p.Title)
            .ToListAsync();

        return pages.Select(p => p.ToDto()).ToList();
    }

    public async Task<List<StorePageResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var pages = await db.StorePages
            .Where(p => p.StoreId == store.Id)
            .OrderBy(p => p.Title)
            .ToListAsync();

        return pages.Select(p => p.ToDto()).ToList();
    }

    public async Task<StorePageResponse> GetBySlugForStoreSlugAsync(string slug, string pageSlug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var page = await db.StorePages.FirstOrDefaultAsync(p => p.Slug == pageSlug && p.StoreId == store.Id)
            ?? throw new NotFoundException("Page not found.");

        return page.ToDto();
    }

    public async Task<StorePageResponse> CreateAsync(string clerkUserId, CreateStorePageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Content is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var now = DateTimeOffset.UtcNow;

        var page = new StorePage
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Title = request.Title.Trim(),
            Slug = await GenerateUniqueSlugAsync(store.Id, request.Title),
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.StorePages.Add(page);
        await db.SaveChangesAsync();

        return page.ToDto();
    }

    public async Task<StorePageResponse> UpdateAsync(string clerkUserId, Guid id, UpdateStorePageRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var page = await db.StorePages.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id)
            ?? throw new NotFoundException("Page not found.");

        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty.");
            page.Title = request.Title.Trim();
            page.Slug = await GenerateUniqueSlugAsync(store.Id, page.Title, page.Id);
        }

        if (request.Content is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                throw new ArgumentException("Content cannot be empty.");
            page.Content = request.Content.Trim();
        }

        page.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return page.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var page = await db.StorePages.FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id)
            ?? throw new NotFoundException("Page not found.");

        db.StorePages.Remove(page);
        await db.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid storeId, string title, Guid? excludeId = null)
    {
        var baseSlug = SlugHelper.Slugify(title, "page");
        var candidate = baseSlug;
        var counter = 2;

        while (await db.StorePages.AnyAsync(p => p.StoreId == storeId && p.Slug == candidate && p.Id != excludeId))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }
}
