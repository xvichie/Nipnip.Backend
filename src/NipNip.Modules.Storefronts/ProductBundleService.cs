using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class ProductBundleService(AppDbContext db, StoreService storeService)
{
    public async Task<List<ProductBundleResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var bundles = await BundlesWithIncludes()
            .Where(b => b.StoreId == store.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bundles.Select(b => b.ToDto()).ToList();
    }

    public async Task<List<ProductBundleResponse>> GetAllForStoreSlugAsync(string slug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var bundles = await BundlesWithIncludes()
            .Where(b => b.StoreId == store.Id && b.IsActive)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return bundles.Select(b => b.ToDto()).ToList();
    }

    public async Task<ProductBundleResponse> GetBySlugForStoreSlugAsync(string slug, string bundleSlug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var bundle = await BundlesWithIncludes()
            .FirstOrDefaultAsync(b => b.StoreId == store.Id && b.Slug == bundleSlug && b.IsActive)
            ?? throw new NotFoundException("Bundle not found.");

        return bundle.ToDto();
    }

    public async Task<ProductBundleResponse> CreateAsync(string clerkUserId, CreateProductBundleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (request.BundlePrice <= 0)
            throw new ArgumentException("Bundle price must be greater than zero.");

        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("A bundle must include at least one product.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        await ValidateItemsAsync(store.Id, request.Items);

        var bundle = new ProductBundle
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(store.Id, request.Name),
            BundlePrice = request.BundlePrice,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.ProductBundles.Add(bundle);

        foreach (var item in request.Items)
        {
            db.ProductBundleItems.Add(new ProductBundleItem { Id = Guid.NewGuid(), BundleId = bundle.Id, ProductId = item.ProductId, Quantity = item.Quantity });
        }

        await db.SaveChangesAsync();

        return (await BundlesWithIncludes().FirstAsync(b => b.Id == bundle.Id)).ToDto();
    }

    public async Task<ProductBundleResponse> UpdateAsync(string clerkUserId, Guid id, UpdateProductBundleRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var bundle = await BundlesWithIncludes().FirstOrDefaultAsync(b => b.Id == id && b.StoreId == store.Id)
            ?? throw new NotFoundException("Bundle not found.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            bundle.Name = request.Name.Trim();
        }

        if (request.BundlePrice.HasValue)
        {
            if (request.BundlePrice.Value <= 0)
                throw new ArgumentException("Bundle price must be greater than zero.");
            bundle.BundlePrice = request.BundlePrice.Value;
        }

        if (request.ImageUrl is not null)
            bundle.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();

        if (request.IsActive.HasValue) bundle.IsActive = request.IsActive.Value;

        if (request.Items is not null)
        {
            if (request.Items.Count == 0)
                throw new ArgumentException("A bundle must include at least one product.");

            await ValidateItemsAsync(store.Id, request.Items);

            db.ProductBundleItems.RemoveRange(bundle.Items);
            foreach (var item in request.Items)
            {
                db.ProductBundleItems.Add(new ProductBundleItem { Id = Guid.NewGuid(), BundleId = bundle.Id, ProductId = item.ProductId, Quantity = item.Quantity });
            }
        }

        await db.SaveChangesAsync();

        return (await BundlesWithIncludes().FirstAsync(b => b.Id == bundle.Id)).ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var bundle = await db.ProductBundles.FirstOrDefaultAsync(b => b.Id == id && b.StoreId == store.Id)
            ?? throw new NotFoundException("Bundle not found.");

        if (await db.OrderBundleItems.AnyAsync(i => i.BundleId == id))
            throw new ConflictException("Cannot delete a bundle referenced by existing orders.");

        db.ProductBundles.Remove(bundle);
        await db.SaveChangesAsync();
    }

    private async Task ValidateItemsAsync(Guid storeId, List<BundleItemInput> items)
    {
        if (items.Select(i => i.ProductId).Distinct().Count() != items.Count)
            throw new ArgumentException("A product cannot appear more than once in the same bundle.");

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero for every bundle item.");

            if (!await db.Products.AnyAsync(p => p.Id == item.ProductId && p.StoreId == storeId))
                throw new NotFoundException("One or more bundle products were not found.");
        }
    }

    private IQueryable<ProductBundle> BundlesWithIncludes() =>
        db.ProductBundles
            .Include(b => b.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images);

    private async Task<string> GenerateUniqueSlugAsync(Guid storeId, string name)
    {
        var baseSlug = SlugHelper.Slugify(name, "bundle");
        var candidate = baseSlug;
        var counter = 2;

        while (await db.ProductBundles.AnyAsync(b => b.StoreId == storeId && b.Slug == candidate))
        {
            candidate = $"{baseSlug}-{counter}";
            counter++;
        }

        return candidate;
    }
}
