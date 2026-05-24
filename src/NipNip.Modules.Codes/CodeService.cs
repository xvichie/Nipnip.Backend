using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Modules.Codes.DTOs;
using NipNip.Modules.Codes.Extensions;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Codes;

public partial class CodeService(AppDbContext db)
{
    [GeneratedRegex(@"^[A-Z0-9]{2,30}$")]
    private static partial Regex CodeRegex();

    public async Task<CheckCodeResponse> CheckAvailabilityAsync(string code, Guid merchantId)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var taken = await db.DiscountCodes
            .AnyAsync(d => d.MerchantId == merchantId && d.Code == normalized);
        return new CheckCodeResponse(!taken);
    }

    public async Task<CodeResponse> ClaimAsync(string clerkUserId, ClaimCodeRequest request)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.Id == request.MerchantId && m.IsActive)
            ?? throw new NotFoundException("Merchant not found.");

        var normalized = request.Code.Trim().ToUpperInvariant();

        if (!CodeRegex().IsMatch(normalized))
            throw new ArgumentException("Code must be 2–30 uppercase letters and digits only.");

        var alreadyClaimed = await db.DiscountCodes
            .AnyAsync(d => d.CreatorId == creator.Id && d.MerchantId == merchant.Id);
        if (alreadyClaimed)
            throw new ConflictException("You already have a discount code for this merchant.");

        var codeTaken = await db.DiscountCodes
            .AnyAsync(d => d.MerchantId == merchant.Id && d.Code == normalized);
        if (codeTaken)
            throw new ConflictException("This code is already taken for this merchant.");

        var discountCode = new DiscountCode
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            MerchantId = merchant.Id,
            Code = normalized,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.DiscountCodes.Add(discountCode);
        await db.SaveChangesAsync();

        return discountCode.ToDto();
    }
}
