using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Payouts.DTOs;
using NipNip.Modules.Payouts.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Payouts;

public class PayoutService(AppDbContext db)
{
    private const decimal MinimumPayoutAmount = 50m;

    public async Task<PayoutResponse> RequestPayoutAsync(string clerkUserId, RequestPayoutRequest request)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        if (request.Amount < MinimumPayoutAmount)
            throw new ArgumentException($"Minimum payout amount is {MinimumPayoutAmount} GEL.");

        // Available = confirmed earnings - already requested/sent amounts
        var totalEarned = await db.Conversions
            .Where(c => c.CreatorId == creator.Id &&
                        (c.Status == ConversionStatus.Confirmed || c.Status == ConversionStatus.Paid))
            .SumAsync(c => (decimal?)c.CreatorEarnings) ?? 0m;

        var alreadyClaimed = await db.Payouts
            .Where(p => p.CreatorId == creator.Id &&
                        (p.Status == PayoutStatus.Requested || p.Status == PayoutStatus.Sent))
            .SumAsync(p => (decimal?)p.RequestedAmount) ?? 0m;

        var available = totalEarned - alreadyClaimed;

        if (request.Amount > available)
            throw new ArgumentException($"Requested amount exceeds available balance ({available:F2} {request.Currency}).");

        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            RequestedAmount = request.Amount,
            Currency = request.Currency,
            Status = PayoutStatus.Requested,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Payouts.Add(payout);
        await db.SaveChangesAsync();

        await db.Entry(payout).Reference(p => p.Creator).LoadAsync();
        return payout.ToDto();
    }

    public async Task<PaginatedResult<PayoutResponse>> GetMyPayoutsAsync(string clerkUserId, int page, int pageSize)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        return (await db.Payouts
            .Where(p => p.CreatorId == creator.Id)
            .Include(p => p.Creator)
            .OrderByDescending(p => p.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize))
            .Map(p => p.ToDto());
    }

    public async Task<decimal> GetAvailableBalanceAsync(string clerkUserId)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var totalEarned = await db.Conversions
            .Where(c => c.CreatorId == creator.Id &&
                        (c.Status == ConversionStatus.Confirmed || c.Status == ConversionStatus.Paid))
            .SumAsync(c => (decimal?)c.CreatorEarnings) ?? 0m;

        var alreadyClaimed = await db.Payouts
            .Where(p => p.CreatorId == creator.Id &&
                        (p.Status == PayoutStatus.Requested || p.Status == PayoutStatus.Sent))
            .SumAsync(p => (decimal?)p.RequestedAmount) ?? 0m;

        return totalEarned - alreadyClaimed;
    }

    public async Task<PaginatedResult<PayoutResponse>> GetAllPayoutsAdminAsync(
        string? status, int page, int pageSize)
    {
        var query = db.Payouts.Include(p => p.Creator).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<PayoutStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        return (await query
            .OrderByDescending(p => p.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize))
            .Map(p => p.ToDto());
    }

    public async Task<PayoutResponse> MarkPayoutSentAsync(Guid id, MarkPayoutSentRequest request)
    {
        var payout = await db.Payouts
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Payout request not found.");

        if (payout.Status != PayoutStatus.Requested)
            throw new ArgumentException("Only Requested payouts can be marked as sent.");

        payout.Status = PayoutStatus.Sent;
        payout.AmountSent = request.AmountSent;
        payout.Notes = request.Notes;
        payout.PaidAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
        return payout.ToDto();
    }

    public async Task<PayoutResponse> RejectPayoutAsync(Guid id, string? notes)
    {
        var payout = await db.Payouts
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Payout request not found.");

        if (payout.Status != PayoutStatus.Requested)
            throw new ArgumentException("Only Requested payouts can be rejected.");

        payout.Status = PayoutStatus.Rejected;
        payout.Notes = notes;

        await db.SaveChangesAsync();
        return payout.ToDto();
    }
}
