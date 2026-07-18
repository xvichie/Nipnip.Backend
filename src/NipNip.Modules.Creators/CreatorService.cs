using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Creators.DTOs;
using NipNip.Modules.Creators.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Creators;

public class CreatorService(AppDbContext db, IConfiguration configuration)
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);

    // Mirrors MerchantService.CanSeeTestMerchantsAsync — test creators stay hidden from the public
    // directory unless the viewer is an admin or the paired test merchant.
    private async Task<bool> CanSeeTestCreatorsAsync(string? callerClerkUserId)
    {
        if (callerClerkUserId is null) return false;

        var adminIds = (configuration["AdminClerkUserIds"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (adminIds.Contains(callerClerkUserId)) return true;

        return await db.Merchants.AnyAsync(m => m.ClerkUserId == callerClerkUserId && m.IsTest);
    }

    public async Task<CreatorResponse> RegisterAsync(string clerkUserId, RegisterCreatorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new ArgumentException("Slug is required.");

        if (!SlugRegex.IsMatch(request.Slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");

        if (await db.Creators.AnyAsync(c => c.ClerkUserId == clerkUserId))
            throw new ConflictException("A creator account already exists for this user.");

        if (await db.Creators.AnyAsync(c => c.Slug == request.Slug))
            throw new ConflictException($"Slug '{request.Slug}' is already taken.");

        var creator = new Creator
        {
            Id = Guid.NewGuid(),
            ClerkUserId = clerkUserId,
            Name = request.Name.Trim(),
            Slug = request.Slug,
            AvatarUrl = request.AvatarUrl,
            InstagramHandle = request.InstagramHandle,
            TiktokHandle = request.TiktokHandle,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Creators.Add(creator);
        await db.SaveChangesAsync();

        return creator.ToDto();
    }

    public async Task<PaginatedResult<CreatorResponse>> GetAllAdminAsync(int page, int pageSize)
    {
        var result = await db.Creators
            .OrderByDescending(c => c.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize);
        return result.Map(c => c.ToDto());
    }

    public async Task<CreatorResponse> GetMeAsync(string clerkUserId)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");
        return creator.ToDto();
    }

    public async Task<PaginatedResult<CreatorResponse>> GetAllPublicAsync(PaginatedRequest request, string? callerClerkUserId)
    {
        var canSeeTest = await CanSeeTestCreatorsAsync(callerClerkUserId);

        var result = await db.Creators
            .Where(c => c.IsActive)
            .Where(c => canSeeTest || !c.IsTest)
            .OrderBy(c => c.Name)
            .ToPaginatedResultAsync(request);
        return result.Map(c => c.ToDto());
    }

    public async Task<List<CreatorResponse>> GetHighlightedAsync(string? callerClerkUserId)
    {
        var canSeeTest = await CanSeeTestCreatorsAsync(callerClerkUserId);

        var creators = await db.Creators
            .Where(c => c.IsActive && c.IsHighlighted)
            .Where(c => canSeeTest || !c.IsTest)
            .OrderBy(c => c.Name)
            .ToListAsync();
        return creators.Select(c => c.ToDto()).ToList();
    }

    public async Task<CreatorResponse> ToggleHighlightAsync(Guid id)
    {
        var creator = await db.Creators.FindAsync(id)
            ?? throw new NotFoundException("Creator not found.");
        creator.IsHighlighted = !creator.IsHighlighted;
        await db.SaveChangesAsync();
        return creator.ToDto();
    }

    public async Task<CreatorResponse> ToggleTestAsync(Guid id)
    {
        var creator = await db.Creators.FindAsync(id)
            ?? throw new NotFoundException("Creator not found.");
        creator.IsTest = !creator.IsTest;
        await db.SaveChangesAsync();
        return creator.ToDto();
    }

    public async Task<CreatorResponse> DeactivateAsync(Guid id)
    {
        var creator = await db.Creators.FindAsync(id)
            ?? throw new NotFoundException("Creator not found.");

        creator.IsActive = false;
        await db.SaveChangesAsync();
        return creator.ToDto();
    }

    public async Task<CreatorResponse> GetBySlugAsync(string slug)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.Slug == slug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{slug}' not found.");

        return creator.ToDto();
    }

    public async Task<CreatorResponse> UpdateAsync(Guid id, string clerkUserId, UpdateCreatorRequest request)
    {
        var creator = await db.Creators.FindAsync(id)
            ?? throw new NotFoundException("Creator not found.");

        if (creator.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only update your own creator profile.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            creator.Name = request.Name.Trim();
        }

        if (request.AvatarUrl is not null) creator.AvatarUrl = request.AvatarUrl;
        if (request.InstagramHandle is not null) creator.InstagramHandle = request.InstagramHandle;
        if (request.InstagramFollowers.HasValue) creator.InstagramFollowers = request.InstagramFollowers;
        if (request.TiktokHandle is not null) creator.TiktokHandle = request.TiktokHandle;
        if (request.TiktokFollowers.HasValue) creator.TiktokFollowers = request.TiktokFollowers;
        if (request.YoutubeHandle is not null) creator.YoutubeHandle = request.YoutubeHandle;
        if (request.YoutubeFollowers.HasValue) creator.YoutubeFollowers = request.YoutubeFollowers;
        if (request.FacebookHandle is not null) creator.FacebookHandle = request.FacebookHandle;
        if (request.FacebookFollowers.HasValue) creator.FacebookFollowers = request.FacebookFollowers;
        if (request.XHandle is not null) creator.XHandle = request.XHandle;
        if (request.XFollowers.HasValue) creator.XFollowers = request.XFollowers;
        if (request.LinkedinHandle is not null) creator.LinkedinHandle = request.LinkedinHandle;
        if (request.LinkedinFollowers.HasValue) creator.LinkedinFollowers = request.LinkedinFollowers;

        await db.SaveChangesAsync();

        return creator.ToDto();
    }

    public async Task<CreatorDashboardResponse> GetDashboardAsync(
        string clerkUserId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var clicksQuery = db.Clicks.Where(c => c.CreatorId == creator.Id);
        var conversionsQuery = db.Conversions.Where(c => c.CreatorId == creator.Id);

        if (from.HasValue)
        {
            clicksQuery = clicksQuery.Where(c => c.ClickedAt >= from.Value);
            conversionsQuery = conversionsQuery.Where(c => c.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            clicksQuery = clicksQuery.Where(c => c.ClickedAt <= to.Value);
            conversionsQuery = conversionsQuery.Where(c => c.CreatedAt <= to.Value);
        }

        var totalClicks = await clicksQuery.CountAsync();
        var totalConversions = await conversionsQuery.CountAsync();

        var totalEarned = await conversionsQuery
            .Where(c => c.Status == ConversionStatus.Confirmed || c.Status == ConversionStatus.Paid)
            .SumAsync(c => (decimal?)c.CreatorEarnings) ?? 0m;

        var topMerchantStats = await conversionsQuery
            .GroupBy(c => c.MerchantId)
            .Select(g => new
            {
                MerchantId = g.Key,
                Conversions = g.Count(),
                CommissionEarned = g.Sum(c => c.CreatorEarnings),
            })
            .OrderByDescending(x => x.CommissionEarned)
            .Take(5)
            .ToListAsync();

        var merchantIds = topMerchantStats.Select(x => x.MerchantId).ToList();

        var merchants = await db.Merchants
            .Where(m => merchantIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        var clicksByMerchant = await clicksQuery
            .Where(c => merchantIds.Contains(c.MerchantId))
            .GroupBy(c => c.MerchantId)
            .Select(g => new { MerchantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MerchantId, x => x.Count);

        var topMerchants = topMerchantStats
            .Where(x => merchants.ContainsKey(x.MerchantId))
            .Select(x => new TopMerchantEntry(
                x.MerchantId,
                merchants[x.MerchantId].Name,
                merchants[x.MerchantId].Slug,
                clicksByMerchant.GetValueOrDefault(x.MerchantId, 0),
                x.Conversions,
                x.CommissionEarned
            ))
            .ToList();

        return new CreatorDashboardResponse(
            totalClicks,
            totalConversions,
            totalEarned,
            topMerchants,
            from,
            to
        );
    }
}
