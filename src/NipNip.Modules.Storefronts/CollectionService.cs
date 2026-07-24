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
            .OrderBy(c => c.Name)
            .ToListAsync();

        return collections.Select(c => c.ToDto()).ToList();
    }

    public async Task<List<CollectionResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var collections = await db.Collections
            .Where(c => c.StoreId == store.Id)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return collections.Select(c => c.ToDto()).ToList();
    }

    public async Task<CollectionResponse> CreateAsync(string clerkUserId, CreateCollectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(store.Id, request.Name),
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

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            collection.Name = request.Name.Trim();
            collection.Slug = await GenerateUniqueSlugAsync(store.Id, collection.Name, collection.Id);
        }

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
