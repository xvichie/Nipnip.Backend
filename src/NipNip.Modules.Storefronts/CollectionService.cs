using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class CollectionService(AppDbContext db, StoreService storeService)
{
    public async Task<List<CollectionResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var collections = await db.Collections
            .Where(c => c.StoreId == store.Id)
            .ToListAsync();

        return collections.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList();
    }

    public async Task<List<CollectionResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var collections = await db.Collections
            .Where(c => c.StoreId == store.Id)
            .ToListAsync();

        return collections.Select(c => c.ToDto()).OrderBy(c => c.Name).ToList();
    }

    private static string? NormalizeName(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // At least one of the three must survive normalization — a collection with no name in any
    // language has nothing to display or slugify from.
    private static (string? Ka, string? En, string? Ru) NormalizeNames(string? nameKa, string? nameEn, string? nameRu)
    {
        var (ka, en, ru) = (NormalizeName(nameKa), NormalizeName(nameEn), NormalizeName(nameRu));
        if (ka is null && en is null && ru is null)
            throw new ArgumentException("At least one language name is required.");
        return (ka, en, ru);
    }

    public async Task<CollectionResponse> CreateAsync(string clerkUserId, CreateCollectionRequest request)
    {
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);

        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            NameKa = nameKa,
            NameEn = nameEn,
            NameRu = nameRu,
            Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!),
        };

        db.Collections.Add(collection);
        await db.SaveChangesAsync();

        return collection.ToDto();
    }

    public async Task<CollectionResponse> UpdateAsync(string clerkUserId, Guid id, UpdateCollectionRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var collection = await db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Collection not found.");

        // Frontend always sends all three name fields on every save (never omits them), same
        // convention as categories/products — unconditional overwrite, not a HasValue-gated
        // partial update.
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);
        collection.NameKa = nameKa;
        collection.NameEn = nameEn;
        collection.NameRu = nameRu;
        collection.Slug = await GenerateUniqueSlugAsync(store.Id, nameKa ?? nameEn ?? nameRu!, collection.Id);

        await db.SaveChangesAsync();
        return collection.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var collection = await db.Collections.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Collection not found.");

        // No block on deleting a collection with products — unlike Category, a collection is
        // just a loose grouping, so its ProductCollection rows simply cascade-delete.
        db.Collections.Remove(collection);
        await db.SaveChangesAsync();
    }

    public async Task<List<ProductSummaryResponse>> GetProductsAsync(string clerkUserId, Guid collectionId)
    {
        await GetOwnCollectionAsync(clerkUserId, collectionId);
        return await GetOrderedProductsAsync(collectionId);
    }

    public async Task<List<ProductSummaryResponse>> SetProductsAsync(string clerkUserId, Guid collectionId, SetCollectionProductsRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        await GetOwnCollectionAsync(clerkUserId, collectionId);

        var distinctIds = request.ProductIds.Distinct().ToList();

        if (distinctIds.Count > 0)
        {
            var validCount = await db.Products.CountAsync(p => distinctIds.Contains(p.Id) && p.StoreId == store.Id);
            if (validCount != distinctIds.Count)
                throw new NotFoundException("One or more selected products were not found in your store.");
        }

        var existing = await db.ProductCollections.Where(pc => pc.CollectionId == collectionId).ToListAsync();
        db.ProductCollections.RemoveRange(existing);

        for (var i = 0; i < distinctIds.Count; i++)
        {
            db.ProductCollections.Add(new ProductCollection
            {
                Id = Guid.NewGuid(),
                ProductId = distinctIds[i],
                CollectionId = collectionId,
                SortOrder = i,
            });
        }

        await db.SaveChangesAsync();

        return await GetOrderedProductsAsync(collectionId);
    }

    private async Task<List<ProductSummaryResponse>> GetOrderedProductsAsync(Guid collectionId)
    {
        var memberships = await db.ProductCollections
            .Where(pc => pc.CollectionId == collectionId)
            .OrderBy(pc => pc.SortOrder)
            .Include(pc => pc.Product).ThenInclude(p => p.Images)
            .ToListAsync();

        return memberships.Select(pc => pc.Product.ToSummaryDto()).ToList();
    }

    private async Task<Collection> GetOwnCollectionAsync(string clerkUserId, Guid collectionId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        return await db.Collections.FirstOrDefaultAsync(c => c.Id == collectionId && c.StoreId == store.Id)
            ?? throw new NotFoundException("Collection not found.");
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid storeId, string name, Guid? excludeId = null)
    {
        var baseSlug = SlugHelper.Slugify(name, "collection");
        var candidate = baseSlug;
        var counter = 2;

        while (await db.Collections.AnyAsync(c => c.StoreId == storeId && c.Slug == candidate && c.Id != excludeId))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }
}
