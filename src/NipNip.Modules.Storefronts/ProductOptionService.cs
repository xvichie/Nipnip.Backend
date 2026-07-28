using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class ProductOptionService(AppDbContext db, ProductService productService)
{
    private static string? NormalizeText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // At least one of the three must survive normalization — same rule as categories/products.
    private static (string? Ka, string? En, string? Ru) NormalizeNames(string? nameKa, string? nameEn, string? nameRu)
    {
        var (ka, en, ru) = (NormalizeText(nameKa), NormalizeText(nameEn), NormalizeText(nameRu));
        if (ka is null && en is null && ru is null)
            throw new ArgumentException("At least one language name is required.");
        return (ka, en, ru);
    }

    public async Task<ProductOptionResponse> CreateOptionAsync(string clerkUserId, Guid productId, CreateProductOptionRequest request)
    {
        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);

        var product = await productService.GetOwnProductAsync(clerkUserId, productId);

        var option = new ProductOption
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            NameKa = nameKa,
            NameEn = nameEn,
            NameRu = nameRu,
        };

        db.ProductOptions.Add(option);
        await db.SaveChangesAsync();

        return option.ToDto();
    }

    // Translation-only update — an option's values are managed separately (Create/DeleteValue),
    // so this never touches them. Adding translations to an EXISTING option (rather than
    // delete+recreate) matters because ProductVariantOptionValue links reference option value
    // IDs — recreating the option would orphan every variant built against its old values.
    public async Task<ProductOptionResponse> UpdateOptionAsync(string clerkUserId, Guid productId, Guid optionId, UpdateProductOptionRequest request)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var option = await db.ProductOptions.Include(o => o.Values)
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId)
            ?? throw new NotFoundException("Product option not found.");

        var (nameKa, nameEn, nameRu) = NormalizeNames(request.NameKa, request.NameEn, request.NameRu);
        option.NameKa = nameKa;
        option.NameEn = nameEn;
        option.NameRu = nameRu;

        await db.SaveChangesAsync();
        return option.ToDto();
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
            ValueKa = NormalizeText(request.ValueKa),
            ValueEn = NormalizeText(request.ValueEn),
            ValueRu = NormalizeText(request.ValueRu),
        };

        db.ProductOptionValues.Add(value);
        await db.SaveChangesAsync();

        return value.ToDto();
    }

    // Translation-only update — the canonical Value is immutable via this endpoint (see
    // UpdateProductOptionValueRequest), so filters/facets and any existing variant selections
    // built against it are never affected by adding or editing translations.
    public async Task<ProductOptionValueResponse> UpdateValueAsync(
        string clerkUserId, Guid productId, Guid optionId, Guid valueId, UpdateProductOptionValueRequest request)
    {
        await productService.GetOwnProductAsync(clerkUserId, productId);

        var value = await db.ProductOptionValues
            .FirstOrDefaultAsync(v => v.Id == valueId && v.ProductOptionId == optionId)
            ?? throw new NotFoundException("Product option value not found.");

        value.ValueKa = NormalizeText(request.ValueKa);
        value.ValueEn = NormalizeText(request.ValueEn);
        value.ValueRu = NormalizeText(request.ValueRu);

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
