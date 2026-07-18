using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

// Merchant-facing CRUD for a product's manual "related products" picks. Mirrors
// ProductOptionService's shape: every method re-verifies ownership via
// ProductService.GetOwnProductAsync before touching anything. The storefront-facing
// read (manual picks, or the category+price fallback) lives directly in ProductService
// instead of here, to avoid a circular dependency between the two services.
public class RelatedProductService(AppDbContext db, ProductService productService)
{
    public async Task<List<ProductSummaryResponse>> GetForOwnProductAsync(string clerkUserId, Guid productId)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);
        return await GetOrderedRelatedAsync(productId);
    }

    public async Task<List<ProductSummaryResponse>> SetForOwnProductAsync(string clerkUserId, Guid productId, SetRelatedProductsRequest request)
    {
        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var distinctIds = request.ProductIds.Distinct().ToList();

        if (distinctIds.Contains(productId))
            throw new ArgumentException("A product cannot be related to itself.");

        if (distinctIds.Count > 0)
        {
            var validCount = await db.Products.CountAsync(p => distinctIds.Contains(p.Id) && p.StoreId == product.StoreId);
            if (validCount != distinctIds.Count)
                throw new NotFoundException("One or more selected products were not found in your store.");
        }

        var existing = await db.ProductRelations.Where(r => r.ProductId == productId).ToListAsync();
        db.ProductRelations.RemoveRange(existing);

        for (var i = 0; i < distinctIds.Count; i++)
        {
            db.ProductRelations.Add(new ProductRelation
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                RelatedProductId = distinctIds[i],
                SortOrder = i,
            });
        }

        await db.SaveChangesAsync();

        return await GetOrderedRelatedAsync(productId);
    }

    private async Task<List<ProductSummaryResponse>> GetOrderedRelatedAsync(Guid productId)
    {
        var relations = await db.ProductRelations
            .Where(r => r.ProductId == productId)
            .OrderBy(r => r.SortOrder)
            .Include(r => r.RelatedProduct).ThenInclude(p => p.Images)
            .ToListAsync();

        return relations.Select(r => r.RelatedProduct.ToSummaryDto()).ToList();
    }
}
