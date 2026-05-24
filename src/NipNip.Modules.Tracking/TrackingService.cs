using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Tracking.DTOs;
using NipNip.Modules.Tracking.Extensions;
using NipNip.Shared.Email;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Tracking;

public class TrackingService(AppDbContext db, IEmailService emailService, ILogger<TrackingService> logger, IOptions<PlatformFeesOptions> platformFees)
{
    public async Task<PaginatedResult<CreatorEarningEntry>> GetMyConversionsAsync(
        string clerkUserId, int page, int pageSize, int? month = null, int? year = null)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var query = db.Conversions
            .Where(c => c.CreatorId == creator.Id)
            .Include(c => c.Merchant)
            .AsQueryable();

        if (year.HasValue && month.HasValue)
        {
            var start = new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1);
            query = query.Where(c => c.CreatedAt >= start && c.CreatedAt < end);
        }
        else if (year.HasValue)
        {
            var start = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(year.Value + 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
            query = query.Where(c => c.CreatedAt >= start && c.CreatedAt < end);
        }

        return (await query
            .OrderByDescending(c => c.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize))
            .Map(c => new CreatorEarningEntry(
                c.Id,
                c.Merchant.Name,
                c.Merchant.Slug,
                c.OrderId,
                c.OrderAmount,
                c.CommissionAmount,
                c.CreatorFeeAmount,
                c.CreatorEarnings,
                c.Currency,
                c.Status.ToString(),
                c.CreatedAt
            ));
    }

    public async Task<List<MonthlyCreatorSummary>> GetMyMonthlySummaryAsync(string clerkUserId)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a creator account.");

        var rows = await db.Conversions
            .Where(c => c.CreatorId == creator.Id)
            .Select(c => new { c.CreatedAt, c.CreatorEarnings, c.CommissionAmount })
            .ToListAsync();

        return rows
            .GroupBy(c => new { c.CreatedAt.UtcDateTime.Year, c.CreatedAt.UtcDateTime.Month })
            .Select(g => new MonthlyCreatorSummary(
                g.Key.Year,
                g.Key.Month,
                Math.Round(g.Sum(c => c.CreatorEarnings), 2),
                Math.Round(g.Sum(c => c.CommissionAmount), 2),
                g.Count()
            ))
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToList();
    }

    public async Task<PaginatedResult<AdminConversionEntry>> GetAllConversionsAdminAsync(
        Guid? merchantId, Guid? creatorId,
        DateTimeOffset? from, DateTimeOffset? to,
        int page, int pageSize)
    {
        var query = db.Conversions
            .Include(c => c.Merchant)
            .Include(c => c.Creator)
            .AsQueryable();

        if (merchantId.HasValue) query = query.Where(c => c.MerchantId == merchantId.Value);
        if (creatorId.HasValue) query = query.Where(c => c.CreatorId == creatorId.Value);
        if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

        return (await query
            .OrderByDescending(c => c.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize))
            .Map(c => new AdminConversionEntry(
                c.Id,
                c.MerchantId, c.Merchant.Name, c.Merchant.Slug,
                c.CreatorId, c.Creator.Name, c.Creator.Slug,
                c.OrderId, c.OrderAmount, c.CommissionAmount,
                c.CreatorFeeAmount, c.MerchantFeeAmount, c.CreatorEarnings,
                c.Currency, c.Source.ToString(), c.Status.ToString(), c.CreatedAt
            ));
    }

    public async Task<PaginatedResult<MerchantConversionEntry>> GetMerchantConversionsAsync(
        string clerkUserId, int page, int pageSize, int? month = null, int? year = null)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var query = db.Conversions
            .Where(c => c.MerchantId == merchant.Id)
            .Include(c => c.Creator)
            .AsQueryable();

        if (year.HasValue && month.HasValue)
        {
            var start = new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1);
            query = query.Where(c => c.CreatedAt >= start && c.CreatedAt < end);
        }
        else if (year.HasValue)
        {
            var start = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(year.Value + 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
            query = query.Where(c => c.CreatedAt >= start && c.CreatedAt < end);
        }

        return (await query
            .OrderByDescending(c => c.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize))
            .Map(c => new MerchantConversionEntry(
                c.Id,
                c.Creator.Name,
                c.Creator.Slug,
                c.OrderId,
                c.OrderAmount,
                c.CommissionAmount,
                c.MerchantFeeAmount,
                c.CommissionAmount + c.MerchantFeeAmount,
                c.Currency,
                c.Source.ToString(),
                c.Status.ToString(),
                c.CreatedAt
            ));
    }

    public async Task<List<MonthlyMerchantSummary>> GetMerchantMonthlySummaryAsync(string clerkUserId)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var rows = await db.Conversions
            .Where(c => c.MerchantId == merchant.Id)
            .Select(c => new { c.CreatedAt, c.CommissionAmount, c.MerchantFeeAmount })
            .ToListAsync();

        return rows
            .GroupBy(c => new { c.CreatedAt.UtcDateTime.Year, c.CreatedAt.UtcDateTime.Month })
            .Select(g => new MonthlyMerchantSummary(
                g.Key.Year,
                g.Key.Month,
                Math.Round(g.Sum(c => c.CommissionAmount + c.MerchantFeeAmount), 2),
                Math.Round(g.Sum(c => c.CommissionAmount), 2),
                g.Count()
            ))
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToList();
    }

    // Called by RedirectController to validate slugs and build the redirect URL.
    // Click logging is done separately via LogClickAsync (fire-and-forget in its own scope).
    public async Task<RedirectInfo> PrepareRedirectAsync(string creatorSlug, string merchantSlug)
    {
        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.Slug == creatorSlug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{creatorSlug}' not found.");

        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.Slug == merchantSlug && m.IsActive)
            ?? throw new NotFoundException($"Merchant '{merchantSlug}' not found.");

        if (string.IsNullOrWhiteSpace(merchant.WebsiteUrl))
            throw new NotFoundException($"Merchant '{merchantSlug}' has no website URL.");

        var refCode = $"{creatorSlug}_{merchantSlug}";
        var separator = merchant.WebsiteUrl.Contains('?') ? '&' : '?';
        var redirectUrl = $"{merchant.WebsiteUrl}{separator}ref={refCode}";

        return new RedirectInfo(redirectUrl, creator.Id, merchant.Id, refCode);
    }

    // Runs in its own scope/lifetime; called fire-and-forget by RedirectController.
    public async Task LogClickAsync(
        Guid creatorId, Guid merchantId, string refCode, string? ipAddress, string? userAgent)
    {
        var click = new Click
        {
            Id = Guid.NewGuid(),
            CreatorId = creatorId,
            MerchantId = merchantId,
            RefCode = refCode,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ClickedAt = DateTimeOffset.UtcNow,
        };

        db.Clicks.Add(click);
        await db.SaveChangesAsync();
    }

    // POST /api/conversions/track — authenticated via X-Merchant-Key header.
    public async Task<ConversionResponse> TrackConversionAsync(string apiKey, TrackConversionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Ref))
            throw new ArgumentException("ref is required.");

        if (string.IsNullOrWhiteSpace(request.OrderId))
            throw new ArgumentException("orderId is required.");

        if (request.Amount <= 0)
            throw new ArgumentException("amount must be greater than zero.");

        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ApiKey == apiKey && m.IsActive)
            ?? throw new NotFoundException("Invalid API key.");

        // ref format: "{creatorSlug}_{merchantSlug}" — underscore is safe because slugs are alphanumeric + hyphens only
        var underscoreIndex = request.Ref.IndexOf('_');
        if (underscoreIndex < 1)
            throw new ArgumentException("Invalid ref format. Expected {creatorSlug}_{merchantSlug}.");

        var creatorSlug = request.Ref[..underscoreIndex];
        var merchantSlugInRef = request.Ref[(underscoreIndex + 1)..];

        if (!string.Equals(merchant.Slug, merchantSlugInRef, StringComparison.Ordinal))
            throw new ArgumentException("ref merchant slug does not match the authenticated merchant.");

        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.Slug == creatorSlug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{creatorSlug}' not found.");

        // Silently return existing conversion on duplicate (same merchant + orderId)
        var existing = await db.Conversions
            .FirstOrDefaultAsync(c => c.MerchantId == merchant.Id && c.OrderId == request.OrderId);

        if (existing is not null)
            return existing.ToDto();

        // Link to the most recent click for this creator+merchant pair
        var click = await db.Clicks
            .Where(c => c.CreatorId == creator.Id && c.MerchantId == merchant.Id)
            .OrderByDescending(c => c.ClickedAt)
            .FirstOrDefaultAsync();

        return await ProcessConversionAsync(
            merchant, creator, click?.Id,
            request.OrderId, request.Amount,
            request.Currency ?? "GEL",
            ConversionSource.Api);
    }

    // POST /api/conversions/manual — authenticated via Clerk JWT (merchant only).
    public async Task<ConversionResponse> ManualConversionAsync(string clerkUserId, ManualConversionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CreatorSlug))
            throw new ArgumentException("creatorSlug is required.");

        if (request.OrderAmount <= 0)
            throw new ArgumentException("orderAmount must be greater than zero.");

        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId && m.IsActive)
            ?? throw new NotFoundException("You don't have an active merchant account.");

        var creator = await db.Creators
            .FirstOrDefaultAsync(c => c.Slug == request.CreatorSlug && c.IsActive)
            ?? throw new NotFoundException($"Creator '{request.CreatorSlug}' not found.");

        var orderId = request.OrderId ?? Guid.NewGuid().ToString("N");

        // Deduplicate only when orderId was explicitly supplied
        if (request.OrderId is not null)
        {
            var existing = await db.Conversions
                .FirstOrDefaultAsync(c => c.MerchantId == merchant.Id && c.OrderId == orderId);

            if (existing is not null)
                return existing.ToDto();
        }

        return await ProcessConversionAsync(
            merchant, creator, clickId: null,
            orderId, request.OrderAmount,
            request.Currency ?? "GEL",
            ConversionSource.ManualReport);
    }

    private async Task<ConversionResponse> ProcessConversionAsync(
        Merchant merchant,
        Creator creator,
        Guid? clickId,
        string orderId,
        decimal orderAmount,
        string currency,
        ConversionSource source)
    {
        var fees = platformFees.Value;
        var commission = Math.Round(orderAmount * merchant.CommissionPercent / 100m, 2);
        var creatorFee = Math.Round(orderAmount * fees.CreatorPlatformFeePercent / 100m, 2);
        var merchantFee = Math.Round(orderAmount * fees.MerchantPlatformFeePercent / 100m, 2);
        var creatorEarnings = commission - creatorFee;

        var conversion = new Conversion
        {
            Id = Guid.NewGuid(),
            MerchantId = merchant.Id,
            CreatorId = creator.Id,
            ClickId = clickId,
            OrderId = orderId,
            OrderAmount = orderAmount,
            CommissionAmount = commission,
            CreatorFeeAmount = creatorFee,
            MerchantFeeAmount = merchantFee,
            CreatorEarnings = creatorEarnings,
            Currency = currency,
            Source = source,
            Status = ConversionStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Conversions.Add(conversion);
        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(merchant.NotificationEmail))
        {
            try
            {
                await emailService.SendConversionNotificationAsync(new ConversionEmailData(
                    merchant.NotificationEmail,
                    merchant.Name,
                    creator.Name,
                    creator.Slug,
                    orderId,
                    orderAmount,
                    commission,
                    currency,
                    conversion.Id.ToString()
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send conversion notification email to {Email}", merchant.NotificationEmail);
            }
        }

        return conversion.ToDto();
    }

    public async Task<string> VerifyApiKeyAsync(string apiKey)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ApiKey == apiKey && m.IsActive)
            ?? throw new NotFoundException("Invalid API key.");

        return merchant.Name;
    }

    public async Task<AdminConversionEntry> GetConversionByIdAsync(string clerkUserId, Guid id)
    {
        var conversion = await db.Conversions
            .Include(c => c.Merchant)
            .Include(c => c.Creator)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Conversion not found.");

        var isCreator = conversion.Creator.ClerkUserId == clerkUserId;
        var isMerchant = conversion.Merchant.ClerkUserId == clerkUserId;

        if (!isCreator && !isMerchant)
            throw new ForbiddenException("You don't have access to this conversion.");

        return new AdminConversionEntry(
            conversion.Id,
            conversion.MerchantId, conversion.Merchant.Name, conversion.Merchant.Slug,
            conversion.CreatorId, conversion.Creator.Name, conversion.Creator.Slug,
            conversion.OrderId, conversion.OrderAmount, conversion.CommissionAmount,
            conversion.CreatorFeeAmount, conversion.MerchantFeeAmount, conversion.CreatorEarnings,
            conversion.Currency, conversion.Source.ToString(), conversion.Status.ToString(), conversion.CreatedAt
        );
    }
}

// Data transfer between PrepareRedirectAsync and the fire-and-forget click logger in RedirectController.
public record RedirectInfo(string RedirectUrl, Guid CreatorId, Guid MerchantId, string RefCode);
