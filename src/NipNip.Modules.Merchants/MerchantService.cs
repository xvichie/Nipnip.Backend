using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Merchants;

public class MerchantService(AppDbContext db)
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9][a-z0-9-]*$", RegexOptions.Compiled);

    public async Task<MerchantResponse> RegisterAsync(string clerkUserId, RegisterMerchantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new ArgumentException("Slug is required.");

        if (!SlugRegex.IsMatch(request.Slug))
            throw new ArgumentException("Slug must be lowercase alphanumeric with optional hyphens and cannot start with a hyphen.");

        if (request.CommissionPercent < 0 || request.CommissionPercent > 100)
            throw new ArgumentException("Commission percent must be between 0 and 100.");

        if (await db.Merchants.AnyAsync(m => m.ClerkUserId == clerkUserId))
            throw new ConflictException("A merchant account already exists for this user.");

        if (await db.Merchants.AnyAsync(m => m.Slug == request.Slug))
            throw new ConflictException($"Slug '{request.Slug}' is already taken.");

        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            ClerkUserId = clerkUserId,
            Name = request.Name.Trim(),
            Slug = request.Slug,
            CommissionPercent = request.CommissionPercent,
            WebsiteUrl = request.WebsiteUrl,
            InstagramHandle = request.InstagramHandle,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            ApiKey = Guid.NewGuid().ToString("N"),
            Balance = 0m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Merchants.Add(merchant);
        await db.SaveChangesAsync();

        return merchant.ToDto();
    }

    public async Task<PaginatedResult<MerchantResponse>> GetAllAdminAsync(int page, int pageSize)
    {
        var result = await db.Merchants
            .OrderByDescending(m => m.CreatedAt)
            .ToPaginatedResultAsync(page, pageSize);
        return result.Map(m => m.ToDto());
    }

    public async Task<MerchantResponse> UpdateAdminAsync(Guid id, UpdateMerchantRequest request)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            merchant.Name = request.Name.Trim();
        }

        if (request.CommissionPercent.HasValue)
        {
            if (request.CommissionPercent.Value < 0 || request.CommissionPercent.Value > 100)
                throw new ArgumentException("Commission percent must be between 0 and 100.");
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }

        if (request.WebsiteUrl is not null) merchant.WebsiteUrl = request.WebsiteUrl;
        if (request.InstagramHandle is not null) merchant.InstagramHandle = request.InstagramHandle;
        if (request.Description is not null) merchant.Description = request.Description;
        if (request.LogoUrl is not null) merchant.LogoUrl = request.LogoUrl;
        if (request.NotificationEmail is not null) merchant.NotificationEmail = request.NotificationEmail;

        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    public async Task<MerchantResponse> DeactivateAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        merchant.IsActive = false;
        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    public async Task<MerchantResponse> GetMeAsync(string clerkUserId)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        return merchant.ToDto();
    }

    public async Task<PaginatedResult<MerchantResponse>> GetAllAsync(PaginatedRequest request)
    {
        var result = await db.Merchants
            .Where(m => m.IsActive && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            .OrderBy(m => m.Name)
            .ToPaginatedResultAsync(request);

        return result.Map(m => m.ToDto());
    }

    public async Task<List<MerchantResponse>> GetHighlightedAsync()
    {
        // A merchant running their own NipNip storefront with affiliate tracking switched off has
        // explicitly opted out — creators must not be able to discover them or generate a link, since
        // any sale a creator drives there would go untracked and uncompensated. Merchants with no
        // storefront at all (WooCommerce/Shopify/custom-site only) are unaffected by this check.
        var merchants = await db.Merchants
            .Where(m => m.IsActive && m.IsHighlighted && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            .OrderBy(m => m.Name)
            .ToListAsync();
        return merchants.Select(m => m.ToDto()).ToList();
    }

    public async Task<MerchantResponse> ToggleHighlightAsync(Guid id)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");
        merchant.IsHighlighted = !merchant.IsHighlighted;
        await db.SaveChangesAsync();
        return merchant.ToDto();
    }

    public async Task<MerchantResponse> GetBySlugAsync(string slug)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.Slug == slug && m.IsActive
                && !db.Stores.Any(s => s.MerchantId == m.Id && !s.AffiliateEnabled))
            ?? throw new NotFoundException($"Merchant '{slug}' not found.");

        return merchant.ToDto();
    }

    public async Task<MerchantResponse> UpdateAsync(Guid id, string clerkUserId, UpdateMerchantRequest request)
    {
        var merchant = await db.Merchants.FindAsync(id)
            ?? throw new NotFoundException("Merchant not found.");

        if (merchant.ClerkUserId != clerkUserId)
            throw new ForbiddenException("You can only update your own merchant profile.");

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Name cannot be empty.");
            merchant.Name = request.Name.Trim();
        }

        if (request.CommissionPercent.HasValue)
        {
            if (request.CommissionPercent.Value < 0 || request.CommissionPercent.Value > 100)
                throw new ArgumentException("Commission percent must be between 0 and 100.");
            merchant.CommissionPercent = request.CommissionPercent.Value;
        }

        if (request.WebsiteUrl is not null) merchant.WebsiteUrl = request.WebsiteUrl;
        if (request.InstagramHandle is not null) merchant.InstagramHandle = request.InstagramHandle;
        if (request.Description is not null) merchant.Description = request.Description;
        if (request.LogoUrl is not null) merchant.LogoUrl = request.LogoUrl;
        if (request.NotificationEmail is not null) merchant.NotificationEmail = request.NotificationEmail;

        await db.SaveChangesAsync();

        return merchant.ToDto();
    }

    public async Task<MerchantSnippetResponse> GetSnippetAsync(string clerkUserId, string apiBaseUrl)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        return new MerchantSnippetResponse(merchant.ApiKey, Snippet1, BuildSnippet2(merchant.ApiKey, apiBaseUrl));
    }

    // Snippet 1: paste on every page — captures the ?ref= URL param into a first-party cookie.
    // No API key needed; safe to expose publicly.
    private static readonly string Snippet1 = """
        <script>
        (function () {
          var p = new URLSearchParams(window.location.search).get('ref');
          if (!p) return;
          document.cookie = '_nn_ref=' + encodeURIComponent(p) + '; path=/; max-age=2592000; SameSite=Lax';
        })();
        </script>
        """;

    // Snippet 2: paste only on the order confirmation page — reads the cookie and reports the conversion.
    private static string BuildSnippet2(string apiKey, string apiBaseUrl) => $$"""
        <script>
        (function () {
          var KEY = '{{apiKey}}';
          var API = '{{apiBaseUrl}}';
          var m = document.cookie.match('(^|;)\\s*_nn_ref\\s*=\\s*([^;]+)');
          if (!m) return;
          var ref = decodeURIComponent(m[2]);

          /* Replace ORDER_ID and AMOUNT with your actual order values */
          fetch(API + '/api/conversions/track', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-Merchant-Key': KEY },
            body: JSON.stringify({ ref: ref, orderId: 'ORDER_ID', amount: 0.00, currency: 'GEL' })
          });
        })();
        </script>
        """;

    public async Task<MerchantDashboardResponse> GetDashboardAsync(
        string clerkUserId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var merchant = await db.Merchants
            .FirstOrDefaultAsync(m => m.ClerkUserId == clerkUserId)
            ?? throw new NotFoundException("You don't have a merchant account.");

        var clicksQuery = db.Clicks.Where(c => c.MerchantId == merchant.Id);
        var conversionsQuery = db.Conversions.Where(c => c.MerchantId == merchant.Id);

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

        var totalOwed = await conversionsQuery
            .Where(c => c.Status == ConversionStatus.Confirmed || c.Status == ConversionStatus.Paid)
            .SumAsync(c => (decimal?)(c.CommissionAmount + c.MerchantFeeAmount)) ?? 0m;

        var topCreatorStats = await conversionsQuery
            .GroupBy(c => c.CreatorId)
            .Select(g => new
            {
                CreatorId = g.Key,
                Conversions = g.Count(),
                TotalOwed = g.Sum(c => c.CommissionAmount + c.MerchantFeeAmount),
            })
            .OrderByDescending(x => x.TotalOwed)
            .Take(5)
            .ToListAsync();

        var creatorIds = topCreatorStats.Select(x => x.CreatorId).ToList();

        var creators = await db.Creators
            .Where(c => creatorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var clicksByCreator = await clicksQuery
            .Where(c => creatorIds.Contains(c.CreatorId))
            .GroupBy(c => c.CreatorId)
            .Select(g => new { CreatorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CreatorId, x => x.Count);

        var topCreators = topCreatorStats
            .Where(x => creators.ContainsKey(x.CreatorId))
            .Select(x => new TopCreatorEntry(
                x.CreatorId,
                creators[x.CreatorId].Name,
                creators[x.CreatorId].Slug,
                clicksByCreator.GetValueOrDefault(x.CreatorId, 0),
                x.Conversions,
                x.TotalOwed
            ))
            .ToList();

        return new MerchantDashboardResponse(
            totalClicks,
            totalConversions,
            totalOwed,
            topCreators,
            from,
            to
        );
    }
}
