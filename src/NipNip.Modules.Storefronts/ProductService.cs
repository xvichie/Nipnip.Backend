using System.Globalization;
using System.Text;
using System.Text.Json;
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
        Guid? collectionId = null,
        bool? isActive = null,
        string? sortBy = null,
        string? sortDir = null)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var query = db.Products
            .Include(p => p.Images)
            .Include(p => p.ProductCollections)
            .Where(p => p.StoreId == store.Id);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (collectionId.HasValue)
            query = query.Where(p => p.ProductCollections.Any(pc => pc.CollectionId == collectionId.Value));

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
        string? collectionSlug = null,
        string? search = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        string? sortDir = null,
        string? optionFilters = null)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var query = db.Products
            .Include(p => p.Images)
            .Include(p => p.ProductCollections)
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

        Guid? matchedCollectionId = null;
        if (!string.IsNullOrWhiteSpace(collectionSlug))
        {
            var collection = await db.Collections.FirstOrDefaultAsync(c => c.StoreId == store.Id && c.Slug == collectionSlug);
            if (collection is null)
                return new PaginatedResult<ProductSummaryResponse>([], 0, pagination.Page, pagination.PageSize, 0);

            matchedCollectionId = collection.Id;
            query = query.Where(p => p.ProductCollections.Any(pc => pc.CollectionId == collection.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        if (minPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => (p.SalePrice ?? p.BasePrice) <= maxPrice.Value);

        // Matched against the product's own declared options/values (not Variants) — a merchant
        // can define "ზომა: 42, 43" on a product without ever using the separate bulk-variant
        // generator, and filtering should still find those products. Each group (one per selected
        // option) is AND'd via the separate .Where calls; values within a group are OR'd via .Any().
        foreach (var group in ParseOptionFilters(optionFilters))
        {
            var name = group.Name;
            var values = group.Values;
            query = query.Where(p => p.Options.Any(o =>
                o.Name == name && o.Values.Any(v => values.Contains(v.Value))));
        }

        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => descending
                ? query.OrderByDescending(p => p.SalePrice ?? p.BasePrice)
                : query.OrderBy(p => p.SalePrice ?? p.BasePrice),
            // No explicit sort requested while filtering by a single collection: show the
            // merchant's own curated order for that collection instead of newest-first.
            null when matchedCollectionId.HasValue =>
                query.OrderBy(p => p.ProductCollections.First(pc => pc.CollectionId == matchedCollectionId.Value).SortOrder),
            _ => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
        };

        var result = await query.ToPaginatedResultAsync(pagination);
        return result.Map(p => p.ToSummaryDto());
    }

    private static readonly JsonSerializerOptions OptionFiltersJsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Malformed/empty input yields no filters rather than an error — a broken filter state on
    // the listing page should show everything, not 500.
    private static List<OptionFilterInput> ParseOptionFilters(string? optionFilters)
    {
        if (string.IsNullOrWhiteSpace(optionFilters)) return [];
        try
        {
            var parsed = JsonSerializer.Deserialize<List<OptionFilterInput>>(optionFilters, OptionFiltersJsonOptions) ?? [];
            return parsed.Where(f => !string.IsNullOrWhiteSpace(f.Name) && f.Values is { Count: > 0 }).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task<List<ProductFacetResponse>> GetFacetsForStoreSlugAsync(string slug, string? categorySlug)
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
                return [];
        }

        var raw = await query
            .SelectMany(p => p.Options)
            .SelectMany(o => o.Values, (o, v) => new { OptionName = o.Name, Value = v.Value })
            .Distinct()
            .ToListAsync();

        return raw
            .GroupBy(x => x.OptionName)
            .Select(g => new ProductFacetResponse(g.Key, OrderFacetValues(g.Select(x => x.Value).Distinct().ToList())))
            .ToList();
    }

    // Numeric-looking values (shoe/clothing sizes: "35", "36"...) sort numerically; everything
    // else (S/M/L/XL, colors) keeps first-seen order rather than alphabetizing, since alphabetical
    // would scramble "S, M, L, XL" into "L, M, S, XL".
    private static List<string> OrderFacetValues(List<string> values)
    {
        if (values.Count > 0 && values.All(v => decimal.TryParse(v, out _)))
            return values.OrderBy(v => decimal.Parse(v)).ToList();
        return values;
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
            .Include(p => p.ProductCollections)
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
                .Include(p => p.ProductCollections)
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
            .Include(p => p.ProductCollections)
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

        if (request.CollectionIds is not null)
            await SyncProductCollectionsAsync(store.Id, product.Id, request.CollectionIds);

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

        if (request.CollectionIds is not null)
        {
            await SyncProductCollectionsAsync(product.StoreId, product.Id, request.CollectionIds);
            product = await GetOwnProductAsync(clerkUserId, id);
        }

        return product.ToDetailDto();
    }

    // Full replace of a product's collection memberships from the product-edit side (as opposed
    // to CollectionService.SetProductsAsync, which replaces a single collection's whole product
    // list from the collection side) — new memberships are appended to the end of each target
    // collection's existing order; memberships this product already had are left untouched.
    private async Task SyncProductCollectionsAsync(Guid storeId, Guid productId, List<Guid> collectionIds)
    {
        var distinctIds = collectionIds.Distinct().ToList();

        if (distinctIds.Count > 0)
        {
            var validCount = await db.Collections.CountAsync(c => distinctIds.Contains(c.Id) && c.StoreId == storeId);
            if (validCount != distinctIds.Count)
                throw new NotFoundException("One or more selected collections were not found in your store.");
        }

        var existing = await db.ProductCollections.Where(pc => pc.ProductId == productId).ToListAsync();
        var existingIds = existing.Select(pc => pc.CollectionId).ToHashSet();

        db.ProductCollections.RemoveRange(existing.Where(pc => !distinctIds.Contains(pc.CollectionId)));

        foreach (var collectionId in distinctIds.Where(cid => !existingIds.Contains(cid)))
        {
            var maxSortOrder = await db.ProductCollections
                .Where(pc => pc.CollectionId == collectionId)
                .Select(pc => (int?)pc.SortOrder)
                .MaxAsync() ?? -1;

            db.ProductCollections.Add(new ProductCollection
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                CollectionId = collectionId,
                SortOrder = maxSortOrder + 1,
            });
        }

        await db.SaveChangesAsync();
    }

    // --- Admin-scoped (building out a prospect's demo store, or support on a real merchant's
    // behalf) — resolves the store by merchantId directly instead of the caller's own Clerk
    // identity. Kept as separate methods rather than refactored into the self-service ones
    // above, so this addition can't change behavior for the existing, already-correct
    // merchant-facing flows that every real store depends on. ---

    public async Task<PaginatedResult<ProductSummaryResponse>> GetAllAdminForMerchantAsync(Guid merchantId, PaginatedRequest pagination)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var result = await db.Products
            .Include(p => p.Images)
            .Where(p => p.StoreId == store.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToPaginatedResultAsync(pagination);

        return result.Map(p => p.ToSummaryDto());
    }

    public async Task<ProductDetailResponse> CreateAdminAsync(Guid merchantId, CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (request.BasePrice < 0)
            throw new ArgumentException("Base price cannot be negative.");

        if (request.SalePrice.HasValue && (request.SalePrice.Value < 0 || request.SalePrice.Value >= request.BasePrice))
            throw new ArgumentException("Sale price must be less than the base price.");

        var store = await storeService.GetStoreForMerchantAsync(merchantId);

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

        // A freshly created product has no images/options/variants yet, so the entity's
        // (empty, initialized-in-place) collections are already accurate — no re-fetch needed.
        return product.ToDetailDto();
    }

    public async Task<ProductDetailResponse> UpdateAdminAsync(Guid merchantId, Guid id, UpdateProductRequest request)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var product = await db.Products
            .Include(p => p.Images)
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues)
            .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id)
            ?? throw new NotFoundException("Product not found.");

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
            if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId.Value && c.StoreId == store.Id))
                throw new NotFoundException("Category not found.");
            product.CategoryId = request.CategoryId;
        }

        if (request.IsActive.HasValue) product.IsActive = request.IsActive.Value;

        await db.SaveChangesAsync();
        return product.ToDetailDto();
    }

    public async Task DeleteAdminAsync(Guid merchantId, Guid id)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        var product = await db.Products.Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id)
            ?? throw new NotFoundException("Product not found.");

        var variantIds = product.Variants.Select(v => v.Id).ToList();
        if (variantIds.Count > 0 && await db.OrderItems.AnyAsync(oi => variantIds.Contains(oi.VariantId)))
            throw new ConflictException("Cannot delete a product with variants referenced by existing orders.");

        if (await db.ProductRelations.AnyAsync(r => r.RelatedProductId == id))
            throw new ConflictException("Cannot delete a product that another product has manually related to it — remove it from that product's related list first.");

        if (await db.ProductBundleItems.AnyAsync(i => i.ProductId == id))
            throw new ConflictException("Cannot delete a product that's part of a bundle — remove it from the bundle first.");

        db.Products.Remove(product);
        await db.SaveChangesAsync();
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

        if (await db.ProductBundleItems.AnyAsync(i => i.ProductId == id))
            throw new ConflictException("Cannot delete a product that's part of a bundle — remove it from the bundle first.");

        db.Products.Remove(product);
        await db.SaveChangesAsync();
    }

    private static readonly string[] ExportColumns = ["Name", "Slug", "CategoryName", "BasePrice", "SalePrice", "Description", "IsActive"];

    public async Task<string> ExportCsvAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var products = await db.Products
            .Include(p => p.Category)
            .Where(p => p.StoreId == store.Id)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(CsvUtil.WriteRow(ExportColumns));

        foreach (var p in products)
        {
            sb.AppendLine(CsvUtil.WriteRow([
                p.Name,
                p.Slug,
                p.Category?.Name ?? "",
                p.BasePrice.ToString(CultureInfo.InvariantCulture),
                p.SalePrice?.ToString(CultureInfo.InvariantCulture) ?? "",
                p.Description ?? "",
                p.IsActive.ToString(),
            ]));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Matches existing products by Slug when the row has one, otherwise by case-insensitive
    /// Name — so a merchant can re-export, tweak prices in a spreadsheet, and re-import to
    /// update in bulk, or add brand-new rows (blank Slug) to create products. Variants/images/
    /// options stay UI-managed — this only covers the flat catalog fields.
    /// </summary>
    public async Task<ProductImportResult> ImportCsvAsync(string clerkUserId, string csvContent)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var rows = CsvUtil.ParseRows(csvContent);

        if (rows.Count < 2)
            throw new ArgumentException("CSV has no data rows.");

        var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        var nameIdx = header.IndexOf("name");
        var slugIdx = header.IndexOf("slug");
        var categoryIdx = header.IndexOf("categoryname");
        var basePriceIdx = header.IndexOf("baseprice");
        var salePriceIdx = header.IndexOf("saleprice");
        var descriptionIdx = header.IndexOf("description");
        var isActiveIdx = header.IndexOf("isactive");

        if (nameIdx == -1 || basePriceIdx == -1)
            throw new ArgumentException("CSV must include at least Name and BasePrice columns.");

        var categories = await db.Categories.Where(c => c.StoreId == store.Id).ToListAsync();
        var existingProducts = await db.Products.Where(p => p.StoreId == store.Id).ToListAsync();

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var results = new List<ProductImportRowResult>();

        for (var r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            var rowNumber = r + 1;
            string Get(int idx) => idx >= 0 && idx < row.Length ? row[idx].Trim() : "";

            var name = Get(nameIdx);
            if (string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                results.Add(new ProductImportRowResult(rowNumber, "", "skipped", "Name is required."));
                continue;
            }

            if (!decimal.TryParse(Get(basePriceIdx), NumberStyles.Any, CultureInfo.InvariantCulture, out var basePrice) || basePrice < 0)
            {
                skipped++;
                results.Add(new ProductImportRowResult(rowNumber, name, "skipped", "Invalid or missing BasePrice."));
                continue;
            }

            decimal? salePrice = null;
            var salePriceRaw = Get(salePriceIdx);
            if (salePriceIdx != -1 && !string.IsNullOrWhiteSpace(salePriceRaw))
            {
                if (!decimal.TryParse(salePriceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var sp) || sp < 0 || sp >= basePrice)
                {
                    skipped++;
                    results.Add(new ProductImportRowResult(rowNumber, name, "skipped", "SalePrice must be less than BasePrice."));
                    continue;
                }
                salePrice = sp;
            }

            Guid? categoryId = null;
            var categoryName = Get(categoryIdx);
            if (categoryIdx != -1 && !string.IsNullOrWhiteSpace(categoryName))
            {
                var match = categories.FirstOrDefault(c => string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
                if (match is null)
                    results.Add(new ProductImportRowResult(rowNumber, name, "warning", $"Category '{categoryName}' not found — left uncategorized."));
                else
                    categoryId = match.Id;
            }

            var description = descriptionIdx != -1 ? Get(descriptionIdx) : "";
            var isActive = isActiveIdx == -1 || !bool.TryParse(Get(isActiveIdx), out var parsedActive) || parsedActive;

            var slug = Get(slugIdx);
            var existing = !string.IsNullOrWhiteSpace(slug)
                ? existingProducts.FirstOrDefault(p => p.Slug == slug)
                : existingProducts.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                existing.Name = name;
                existing.BasePrice = basePrice;
                existing.SalePrice = salePrice;
                if (categoryId.HasValue) existing.CategoryId = categoryId;
                if (!string.IsNullOrWhiteSpace(description)) existing.Description = description;
                existing.IsActive = isActive;
                updated++;
                results.Add(new ProductImportRowResult(rowNumber, name, "updated", null));
            }
            else
            {
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    StoreId = store.Id,
                    CategoryId = categoryId,
                    Name = name,
                    Slug = await GenerateUniqueSlugAsync(store.Id, name),
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    BasePrice = basePrice,
                    SalePrice = salePrice,
                    IsActive = isActive,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.Products.Add(product);
                existingProducts.Add(product);
                created++;
                results.Add(new ProductImportRowResult(rowNumber, name, "created", null));
            }
        }

        await db.SaveChangesAsync();
        return new ProductImportResult(created, updated, skipped, results);
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
            .Include(p => p.ProductCollections)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == store.Id)
            ?? throw new NotFoundException("Product not found.");
    }

    // Admin-scoped equivalent of GetOwnProductAsync — resolves by merchantId instead of the
    // caller's own Clerk identity.
    internal async Task<Product> GetProductForMerchantAsync(Guid merchantId, Guid productId)
    {
        var store = await storeService.GetStoreForMerchantAsync(merchantId);

        return await db.Products
            .Include(p => p.Images)
            .Include(p => p.Options).ThenInclude(o => o.Values)
            .Include(p => p.Variants).ThenInclude(v => v.OptionValues)
            .Include(p => p.ProductCollections)
            .FirstOrDefaultAsync(p => p.Id == productId && p.StoreId == store.Id)
            ?? throw new NotFoundException("Product not found.");
    }
}
