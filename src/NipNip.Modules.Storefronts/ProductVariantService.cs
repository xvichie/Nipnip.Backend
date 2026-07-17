using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class ProductVariantService(AppDbContext db, ProductService productService)
{
    public async Task<ProductVariantResponse> CreateAsync(string clerkUserId, Guid productId, CreateProductVariantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku))
            throw new ArgumentException("Sku is required.");

        if (request.Price < 0)
            throw new ArgumentException("Price cannot be negative.");

        if (request.SalePrice.HasValue && (request.SalePrice.Value < 0 || request.SalePrice.Value >= request.Price))
            throw new ArgumentException("Sale price must be less than the regular price.");

        if (request.Stock is < 0)
            throw new ArgumentException("Stock cannot be negative.");

        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        if (await db.ProductVariants.AnyAsync(v => v.ProductId == productId && v.Sku == request.Sku))
            throw new ConflictException($"Sku '{request.Sku}' is already used by another variant of this product.");

        var optionValueIds = request.OptionValueIds.Distinct().ToList();
        if (optionValueIds.Count > 0)
        {
            var validCount = await db.ProductOptionValues
                .CountAsync(v => optionValueIds.Contains(v.Id) && v.ProductOption.ProductId == productId);
            if (validCount != optionValueIds.Count)
                throw new ArgumentException("One or more option values do not belong to this product.");
        }

        var configuredOptions = product.Options.Where(o => o.Values.Count > 0).ToList();
        if (configuredOptions.Count > 0)
        {
            var optionIdsCovered = configuredOptions
                .Where(o => o.Values.Any(v => optionValueIds.Contains(v.Id)))
                .Select(o => o.Id)
                .Distinct()
                .Count();
            if (optionIdsCovered != configuredOptions.Count || optionValueIds.Count != configuredOptions.Count)
                throw new ArgumentException("Select exactly one value for every product option.");
        }

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = request.Sku.Trim(),
            Price = request.Price,
            SalePrice = request.SalePrice,
            Stock = request.Stock,
        };

        db.ProductVariants.Add(variant);

        foreach (var optionValueId in optionValueIds)
        {
            db.ProductVariantOptionValues.Add(new ProductVariantOptionValue
            {
                VariantId = variant.Id,
                OptionValueId = optionValueId,
            });
        }

        await db.SaveChangesAsync();

        variant = await db.ProductVariants
            .Include(v => v.OptionValues)
            .FirstAsync(v => v.Id == variant.Id);

        return variant.ToDto();
    }

    public async Task<ProductVariantResponse> UpdateAsync(
        string clerkUserId, Guid productId, Guid variantId, UpdateProductVariantRequest request)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var variant = await db.ProductVariants
            .Include(v => v.OptionValues)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId)
            ?? throw new NotFoundException("Product variant not found.");

        if (request.Sku is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new ArgumentException("Sku cannot be empty.");

            if (await db.ProductVariants.AnyAsync(v => v.ProductId == productId && v.Sku == request.Sku && v.Id != variantId))
                throw new ConflictException($"Sku '{request.Sku}' is already used by another variant of this product.");

            variant.Sku = request.Sku.Trim();
        }

        if (request.Price.HasValue)
        {
            if (request.Price.Value < 0)
                throw new ArgumentException("Price cannot be negative.");
            variant.Price = request.Price.Value;
        }

        if (request.SalePrice.HasValue)
        {
            if (request.SalePrice.Value < 0 || request.SalePrice.Value >= variant.Price)
                throw new ArgumentException("Sale price must be less than the regular price.");
            variant.SalePrice = request.SalePrice;
        }

        if (request.ClearStock)
        {
            variant.Stock = null;
        }
        else if (request.Stock.HasValue)
        {
            if (request.Stock.Value < 0)
                throw new ArgumentException("Stock cannot be negative.");
            variant.Stock = request.Stock.Value;
        }

        await db.SaveChangesAsync();
        return variant.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid productId, Guid variantId)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var variant = await db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId)
            ?? throw new NotFoundException("Product variant not found.");

        if (await db.OrderItems.AnyAsync(oi => oi.VariantId == variantId))
            throw new ConflictException("Cannot delete a variant that is referenced by existing orders.");

        db.ProductVariants.Remove(variant);
        await db.SaveChangesAsync();
    }
}
