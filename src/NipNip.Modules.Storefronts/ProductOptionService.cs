using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class ProductOptionService(AppDbContext db, ProductService productService)
{
    public async Task<ProductOptionResponse> CreateOptionAsync(string clerkUserId, Guid productId, CreateProductOptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var option = new ProductOption
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Name = request.Name.Trim(),
        };

        db.ProductOptions.Add(option);
        await db.SaveChangesAsync();

        return new ProductOptionResponse(option.Id, option.Name, []);
    }

    public async Task DeleteOptionAsync(string clerkUserId, Guid productId, Guid optionId)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var option = await db.ProductOptions.FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId)
            ?? throw new NotFoundException("Product option not found.");

        db.ProductOptions.Remove(option);
        await db.SaveChangesAsync();
    }

    public async Task<ProductOptionValueResponse> CreateValueAsync(
        string clerkUserId, Guid productId, Guid optionId, CreateProductOptionValueRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            throw new ArgumentException("Value is required.");

        await productService.GetOwnProductAsync(clerkUserId, productId);

        var option = await db.ProductOptions.FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId)
            ?? throw new NotFoundException("Product option not found.");

        var value = new ProductOptionValue
        {
            Id = Guid.NewGuid(),
            ProductOptionId = option.Id,
            Value = request.Value.Trim(),
        };

        db.ProductOptionValues.Add(value);
        await db.SaveChangesAsync();

        return value.ToDto();
    }

    public async Task DeleteValueAsync(string clerkUserId, Guid productId, Guid optionId, Guid valueId)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var value = await db.ProductOptionValues
            .FirstOrDefaultAsync(v => v.Id == valueId && v.ProductOptionId == optionId)
            ?? throw new NotFoundException("Product option value not found.");

        db.ProductOptionValues.Remove(value);
        await db.SaveChangesAsync();
    }
}
