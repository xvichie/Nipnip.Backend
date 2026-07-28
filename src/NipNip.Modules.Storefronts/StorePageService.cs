using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class StorePageService(AppDbContext db, StoreService storeService)
{
    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // At least one of the three must survive normalization — a page with nothing to show in any
    // language has no reason to exist. Same rule applied to both Title and Content.
    private static (string? Ka, string? En, string? Ru) NormalizeTriple(string? ka, string? en, string? ru, string fieldName)
    {
        var (nKa, nEn, nRu) = (NormalizeText(ka), NormalizeText(en), NormalizeText(ru));
        if (nKa is null && nEn is null && nRu is null)
            throw new ArgumentException($"At least one language {fieldName} is required.");
        return (nKa, nEn, nRu);
    }

    public async Task<List<StorePageResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var pages = await db.StorePages
            .Where(p => p.StoreId == store.Id)
            .ToListAsync();

        return pages.Select(p => p.ToDto()).OrderBy(p => p.Title).ToList();
    }

    public async Task<List<StorePageResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var pages = await db.StorePages
            .Where(p => p.StoreId == store.Id)
            .ToListAsync();

        return pages.Select(p => p.ToDto()).OrderBy(p => p.Title).ToList();
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
        var (titleKa, titleEn, titleRu) = NormalizeTriple(request.TitleKa, request.TitleEn, request.TitleRu, "title");
        var (contentKa, contentEn, contentRu) = NormalizeTriple(request.ContentKa, request.ContentEn, request.ContentRu, "content");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var now = DateTimeOffset.UtcNow;

        var page = new StorePage
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            TitleKa = titleKa,
            TitleEn = titleEn,
            TitleRu = titleRu,
            Slug = await GenerateUniqueSlugAsync(store.Id, titleKa ?? titleEn ?? titleRu!),
            ContentKa = contentKa,
            ContentEn = contentEn,
            ContentRu = contentRu,
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

        // Frontend always sends all six title/content fields on every save (never omits them),
        // same convention as categories/products — unconditional overwrite, not a HasValue-gated
        // partial update.
        var (titleKa, titleEn, titleRu) = NormalizeTriple(request.TitleKa, request.TitleEn, request.TitleRu, "title");
        page.TitleKa = titleKa;
        page.TitleEn = titleEn;
        page.TitleRu = titleRu;
        page.Slug = await GenerateUniqueSlugAsync(store.Id, titleKa ?? titleEn ?? titleRu!, page.Id);

        var (contentKa, contentEn, contentRu) = NormalizeTriple(request.ContentKa, request.ContentEn, request.ContentRu, "content");
        page.ContentKa = contentKa;
        page.ContentEn = contentEn;
        page.ContentRu = contentRu;

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
