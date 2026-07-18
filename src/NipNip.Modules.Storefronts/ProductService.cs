using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Extensions;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Storefronts;

public class ProductService(AppDbContext db, StoreService storeService)
{
    private const decimal RelatedPriceBracket = 0.3m;
    private const int RelatedProductsLimit = 8;

    public async Task<PaginatedResult<ProductSummaryResponse>> GetAllForOwnStoreAsync(
        string clerkUserId,
        PaginatedRequest pagination,
        string? search = null,
        Guid? categoryId = null,
        bool? isActive = null,
        string? sortBy = null,
        string? sortDir = null)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var query = db.Products
            .Include(p => p.Images)
            .Where(p => p.StoreId == store.Id);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => descending ? query.OrderByDescending(p => p.BasePrice) : query.OrderBy(p => p.BasePrice),
            _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        };

        var result = await query.ToPaginatedResultAsync(pagination);

        return result.Map(p => p.ToSummaryDto());
    }

    public async Task<PaginatedResult<ProductSummaryResponse>> GetAllForStoreSlugAsync(
        string slug,
        PaginatedRequest pagination,
        string? categorySlug = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortDir = null)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var query = db.Products
            .Include(p => p.Images)
            .Where(p => p.StoreId == store.Id && p.IsActive);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.StoreId == store.Id && c.Slug == categorySlug);
            if (category is not null)
            {
                query = query.Where(p => p.CategoryId == category.Id);
            }
            else if (categorySlug == "sale")
            {
                query = query.Where(p => p.SalePrice != null || p.Variants.Any(v => v.SalePrice != null));
            }
            else
            {
                return new PaginatedResult<ProductSummaryResponse>([], 0, pagination.Page, pagination.PageSize, 0);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        if (minPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= maxPrice.Value);

        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => descending
                ? query.OrderByDescending(p => p.SalePrice ?? p.BasePrice)
                : query.OrderBy(p => p.SalePrice ?? p.BasePrice),
            _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        };

        var result = await query.ToPaginatedResultAsync(pagination);
        return result.Map(p => p.ToSummaryDto());
    }

    public async Task<ProductPriceRangeResponse> GetPriceRangeForStoreSlugAsync(string slug, string? categorySlug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var query = db.Products.Where(p => p.StoreId == store.Id && p.IsActive);

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.StoreId == store.Id && c.Slug == categorySlug);
            if (category is not null)
                query = query.Where(p => p.CategoryId == category.Id);
            else if (categorySlug == "sale")
                query = query.Where(p => p.SalePrice != null || p.Variants.Any(v => v.SalePrice != null));
            else
                return new ProductPriceRangeResponse(0, 0);
        }

        var prices = await query.Select(p => p.SalePrice ?? p.BasePrice).ToListAsync();
        if (prices.Count == 0) return new ProductPriceRangeResponse(0, 0);

        return new ProductPriceRangeResponse(Math.Floor(prices.Min()), Math.Ceiling(prices.Max()));
    }

    public async Task<ProductDetailResponse> GetBySlugForStoreSlugAsync(string slug, string productSlug)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var product = await db.Products
            .Include(p => p.Images)
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues)
            .FirstOrDefaultAsync(p => p.Slug == productSlug && p.StoreId == store.Id && p.IsActive)
            ?? throw new NotFoundException("Product not found.");

        var relatedProducts = await GetRelatedProductsAsync(product);

        return product.ToDetailDto(relatedProducts);
    }

    // A merchant's manual picks win outright (in their chosen order) if any exist;
    // otherwise falls back to same category + a price bracket around this product's
    // own price, closest-price-first. Manual picks are looked up first specifically so
    // the (usually cheap) fallback query never runs when it isn't needed.
    private async Task<List<ProductSummaryResponse>> GetRelatedProductsAsync(Product product)
    {
        var manualOrder = await db.ProductRelations
            .Where(r => r.ProductId == product.Id)
            .OrderBy(r => r.SortOrder)
            .Select(r => r.RelatedProductId)
            .ToListAsync();

        if (manualOrder.Count > 0)
        {
            var manualProducts = await db.Products
                .Include(p => p.Images)
                .Where(p => manualOrder.Contains(p.Id) && p.IsActive)
                .ToDictionaryAsync(p => p.Id);

            return manualOrder
                .Where(manualProducts.ContainsKey)
                .Select(id => manualProducts[id].ToSummaryDto())
                .ToList();
        }

        var effectivePrice = product.SalePrice ?? product.BasePrice;
        var minPrice = effectivePrice * (1 - RelatedPriceBracket);
        var maxPrice = effectivePrice * (1 + RelatedPriceBracket);

        var fallbackQuery = db.Products
            .Include(p => p.Images)
            .Where(p => p.StoreId == product.StoreId && p.Id != product.Id && p.IsActive);

        if (product.CategoryId.HasValue)
            fallbackQuery = fallbackQuery.Where(p => p.CategoryId == product.CategoryId);

        var candidates = await fallbackQuery
            .Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice && (p.SalePrice ?? p.BasePrice) <= maxPrice)
            .ToListAsync();

        return candidates
            .OrderBy(p => Math.Abs((p.SalePrice ?? p.BasePrice) - effectivePrice))
            .Take(RelatedProductsLimit)
            .Select(p => p.ToSummaryDto())
            .ToList();
    }

    public async Task<ProductDetailResponse> GetOwnByIdAsync(string clerkUserId, Guid id)
    {
        var product = await GetOwnProductAsync(clerkUserId, id);
        return product.ToDetailDto();
    }

    public async Task<ProductDetailResponse> CreateAsync(string clerkUserId, CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (request.BasePrice < 0)
            throw new ArgumentException("Base price cannot be negative.");

        if (request.SalePrice.HasValue && (request.SalePrice.Value < 0 || request.SalePrice.Value >= request.BasePrice))
            throw new ArgumentException("Sale price must be less than the base price.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        if (request.CategoryId.HasValue &&
            !await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.StoreId == store.Id))
            throw new NotFoundException("Category not found.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Slug = await GenerateUniqueSlugAsync(store.Id, request.Name),
            Description = request.Description,
            VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim(),
            BasePrice = request.BasePrice,
            SalePrice = request.SalePrice,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return (await GetOwnProductAsync(clerkUserId, product.Id)).ToDetailDto();
    }

    public async Task<ProductDetailResponse> UpdateAsync(string clerkUserId, Guid id, UpdateProductRequest request)
    {
        var product = await GetOwnProductAsync(clerkUserId, id);

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            product.Name = request.Name.Trim();
        }

        if (request.Description is not null) product.Description = request.Description;

        if (request.VideoUrl is not null)
            product.VideoUrl = string.IsNullOrWhiteSpace(request.VideoUrl) ? null : request.VideoUrl.Trim();

        if (request.BasePrice.HasValue)
        {
            if (request.BasePrice.Value < 0)
                throw new ArgumentException("Base price cannot be negative.");
            product.BasePrice = request.BasePrice.Value;
        }

        if (request.SalePrice.HasValue)
        {
            if (request.SalePrice.Value < 0 || request.SalePrice.Value >= product.BasePrice)
                throw new ArgumentException("Sale price must be less than the base price.");
            product.SalePrice = request.SalePrice;
        }

        if (request.CategoryId.HasValue)
        {
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.StoreId == product.StoreId))
                throw new NotFoundException("Category not found.");
            product.CategoryId = request.CategoryId;
        }

        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync();
        return product.ToDetailDto();
    }

    public async Task<ProductDetailResponse> DuplicateAsync(string clerkUserId, Guid id)
    {
        var original = await GetOwnProductAsync(clerkUserId, id);

        var clone = new Product
        {
            Id = Guid.NewGuid(),
            StoreId = original.StoreId,
            CategoryId = original.CategoryId,
            Name = $"{original.Name} (Copy)",
            Slug = await GenerateUniqueSlugAsync(original.StoreId, original.Name),
            Description = original.Description,
            VideoUrl = original.VideoUrl,
            BasePrice = original.BasePrice,
            SalePrice = original.SalePrice,
            IsActive = original.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Products.Add(clone);

        foreach (var image in original.Images)
        {
            db.ProductImages.Add(new ProductImage
            {
                Id = Guid.NewGuid(),
                ProductId = clone.Id,
                Url = image.Url,
                SortOrder = image.SortOrder,
            });
        }

        var valueIdMap = new Dictionary<Guid, Guid>();

        foreach (var option in original.Options)
        {
            var newOption = new ProductOption
            {
                Id = Guid.NewGuid(),
                ProductId = clone.Id,
                Name = option.Name,
            };
            db.ProductOptions.Add(newOption);

            foreach (var value in option.Values)
            {
                var newValueId = Guid.NewGuid();
                valueIdMap[value.Id] = newValueId;

                db.ProductOptionValues.Add(new ProductOptionValue
                {
                    Id = newValueId,
                    ProductOptionId = newOption.Id,
                    Value = value.Value,
                });
            }
        }

        foreach (var variant in original.Variants)
        {
            var newVariant = new ProductVariant
            {
                Id = Guid.NewGuid(),
                ProductId = clone.Id,
                Sku = variant.Sku,
                Price = variant.Price,
                SalePrice = variant.SalePrice,
                Stock = variant.Stock,
            };
            db.ProductVariants.Add(newVariant);

            foreach (var optionValue in variant.OptionValues)
            {
                db.ProductVariantOptionValues.Add(new ProductVariantOptionValue
                {
                    VariantId = newVariant.Id,
                    OptionValueId = valueIdMap[optionValue.OptionValueId],
                });
            }
        }

        await db.SaveChangesAsync();

        return (await GetOwnProductAsync(clerkUserId, clone.Id)).ToDetailDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var product = await GetOwnProductAsync(clerkUserId, id);

        var variantIds = product.Variants.Select(v => v.Id).ToList();
        if (variantIds.Count > 0 && await db.OrderItems.AnyAsync(oi => variantIds.Contains(oi.VariantId)))
            throw new ConflictException("Cannot delete a product with variants referenced by existing orders.");

        if (await db.ProductRelations.AnyAsync(r => r.RelatedProductId == id))
            throw new ConflictException("Cannot delete a product that another product has manually related to it — remove it from that product's related list first.");

        db.Products.Remove(product);
        await db.SaveChangesAsync();
    }

    private async Task<string> GenerateUniqueSlugAsync(Guid storeId, string name)
    {
        var baseSlug = SlugHelper.Slugify(name);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var candidate = SlugHelper.WithRandomSuffix(baseSlug);
            if (!await db.Products.AnyAsync(p => p.StoreId == storeId && p.Slug == candidate))
                return candidate;
        }

        throw new InvalidOperationException("Could not generate a unique product slug.");
    }

    internal async Task<Product> GetOwnProductAsync(string clerkUserId, Guid productId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        return await db.Products
            .Include(p => p.Images)
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == store.Id)
            ?? throw new NotFoundException("Product not found.");
    }
}
