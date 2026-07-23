using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class ProductImageService(AppDbContext db, ProductService productService)
{
    public async Task<ProductImageResponse> CreateAsync(string clerkUserId, Guid productId, CreateProductImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("Url is required.");

        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var sortOrder = await db.ProductImages.Where(i => i.ProductId == productId).CountAsync();

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Url = request.Url,
            SortOrder = sortOrder,
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync();

        return image.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid productId, Guid imageId)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId)
            ?? throw new NotFoundException("Product image not found.");

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
    }

    // Admin-scoped — see ProductService's admin methods for why this isn't merged into the
    // self-service one above.
    public async Task<ProductImageResponse> CreateAdminAsync(Guid merchantId, Guid productId, CreateProductImageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("Url is required.");

        var product = await productService.GetProductForMerchantAsync(merchantId, productId);

        var sortOrder = await db.ProductImages.Where(i => i.ProductId == productId).CountAsync();

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Url = request.Url,
            SortOrder = sortOrder,
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync();

        return image.ToDto();
    }

    public async Task DeleteAdminAsync(Guid merchantId, Guid productId, Guid imageId)
    {
        await productService.GetProductForMerchantAsync(merchantId, productId);

        var image = await db.ProductImages.FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId)
            ?? throw new NotFoundException("Product image not found.");

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync();
    }

    public async Task ReorderAsync(string clerkUserId, Guid productId, List<Guid> imageIds)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var images = await db.ProductImages.Where(i => i.ProductId == productId).ToListAsync();

        if (images.Count != imageIds.Count || !images.Select(i => i.Id).ToHashSet().SetEquals(imageIds))
            throw new ArgumentException("imageIds must match this product's existing images exactly.");

        for (var index = 0; index < imageIds.Count; index++)
        {
            var image = images.First(i => i.Id == imageIds[index]);
            image.SortOrder = index;
        }

        await db.SaveChangesAsync();
    }
}
