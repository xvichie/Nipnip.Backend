using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts;

public class StoreDiscountCodeService(AppDbContext db, StoreService storeService)
{
    public async Task<List<StoreDiscountCodeResponse>> GetAllForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var codes = await db.StoreDiscountCodes
            .Where(c => c.StoreId == store.Id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return codes.Select(c => c.ToDto()).ToList();
    }

    public async Task<StoreDiscountCodeResponse> CreateAsync(string clerkUserId, CreateStoreDiscountCodeRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var (type, normalizedCode) = ValidateFields(request.Code, request.Type, request.Value);

        if (await db.StoreDiscountCodes.AnyAsync(c => c.StoreId == store.Id && c.Code == normalizedCode))
            throw new ArgumentException("A discount code with this code already exists.");

        var entity = new StoreDiscountCode
        {
            Id = Guid.NewGuid(),
            StoreId = store.Id,
            Code = normalizedCode,
            Type = type,
            Value = request.Value,
            MinOrderAmount = request.MinOrderAmount,
            MaxUses = request.MaxUses,
            UsesCount = 0,
            ExpiresAt = request.ExpiresAt,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.StoreDiscountCodes.Add(entity);
        await db.SaveChangesAsync();

        return entity.ToDto();
    }

    public async Task<StoreDiscountCodeResponse> UpdateAsync(string clerkUserId, Guid id, UpdateStoreDiscountCodeRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var entity = await db.StoreDiscountCodes.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Discount code not found.");

        var (type, normalizedCode) = ValidateFields(request.Code, request.Type, request.Value);

        if (await db.StoreDiscountCodes.AnyAsync(c => c.StoreId == store.Id && c.Code == normalizedCode && c.Id != id))
            throw new ArgumentException("A discount code with this code already exists.");

        entity.Code = normalizedCode;
        entity.Type = type;
        entity.Value = request.Value;
        entity.MinOrderAmount = request.MinOrderAmount;
        entity.MaxUses = request.MaxUses;
        entity.ExpiresAt = request.ExpiresAt;
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return entity.ToDto();
    }

    public async Task DeleteAsync(string clerkUserId, Guid id)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var entity = await db.StoreDiscountCodes.FirstOrDefaultAsync(c => c.Id == id && c.StoreId == store.Id)
            ?? throw new NotFoundException("Discount code not found.");

        db.StoreDiscountCodes.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<ValidateDiscountCodeResponse> ValidateAsync(string slug, ValidateDiscountCodeRequest request)
    {
        var store = await db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive)
            ?? throw new NotFoundException($"Store '{slug}' not found.");

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var entity = await db.StoreDiscountCodes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.StoreId == store.Id && c.Code == normalizedCode);

        var errorCode = Evaluate(entity, request.Subtotal);
        if (errorCode is not null || entity is null)
            return new ValidateDiscountCodeResponse(false, 0m, errorCode ?? "not_found", entity?.MinOrderAmount);

        return new ValidateDiscountCodeResponse(true, ComputeDiscountAmount(entity, request.Subtotal), null, entity.MinOrderAmount);
    }

    /// <summary>
    /// Re-validates and applies the code at the moment of checkout — never trusts the amount
    /// a client may have echoed back from an earlier /validate call — and increments UsesCount
    /// on the tracked entity so it's persisted by the caller's own SaveChangesAsync alongside
    /// the new Order (single transaction, no separate round-trip).
    /// </summary>
    public async Task<(decimal Amount, string? Code)> ApplyForCheckoutAsync(Guid storeId, string? code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (0m, null);

        var normalizedCode = code.Trim().ToUpperInvariant();
        var entity = await db.StoreDiscountCodes.FirstOrDefaultAsync(c => c.StoreId == storeId && c.Code == normalizedCode);

        if (Evaluate(entity, subtotal) is { } errorCode)
            throw new ArgumentException($"Discount code is not valid: {errorCode}.");

        var amount = ComputeDiscountAmount(entity!, subtotal);
        entity!.UsesCount += 1;

        return (amount, entity.Code);
    }

    private static (DiscountCodeType Type, string NormalizedCode) ValidateFields(string code, string type, decimal value)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.");

        if (!Enum.TryParse<DiscountCodeType>(type, true, out var parsedType))
            throw new ArgumentException("Type must be 'Percentage' or 'FixedAmount'.");

        if (value <= 0)
            throw new ArgumentException("Value must be greater than zero.");

        if (parsedType == DiscountCodeType.Percentage && value > 100)
            throw new ArgumentException("Percentage value cannot exceed 100.");

        return (parsedType, code.Trim().ToUpperInvariant());
    }

    private static string? Evaluate(StoreDiscountCode? entity, decimal subtotal)
    {
        if (entity is null) return "not_found";
        if (!entity.IsActive) return "inactive";
        if (entity.ExpiresAt is { } exp && exp < DateTimeOffset.UtcNow) return "expired";
        if (entity.MaxUses is { } max && entity.UsesCount >= max) return "max_uses";
        if (entity.MinOrderAmount is { } min && subtotal < min) return "min_order";
        return null;
    }

    private static decimal ComputeDiscountAmount(StoreDiscountCode entity, decimal subtotal)
    {
        var amount = entity.Type == DiscountCodeType.Percentage
            ? subtotal * (entity.Value / 100m)
            : entity.Value;
        return Math.Min(amount, subtotal);
    }
}
