using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts;

public class StoreAnalyticsService(AppDbContext db, StoreService storeService)
{
    private const int MaxPathLength = 500;
    private const int TopPagesLimit = 10;

    // Maps a referring hostname to a human-readable source label. Anything not listed here
    // falls back to showing the raw hostname, so unfamiliar traffic sources still show up
    // usefully instead of getting silently dropped into a generic bucket.
    private static readonly Dictionary<string, string> KnownReferrers = new()
    {
        ["instagram.com"] = "Instagram",
        ["facebook.com"] = "Facebook",
        ["l.facebook.com"] = "Facebook",
        ["m.facebook.com"] = "Facebook",
        ["tiktok.com"] = "TikTok",
        ["google.com"] = "Google",
        ["t.co"] = "Twitter / X",
        ["twitter.com"] = "Twitter / X",
        ["x.com"] = "Twitter / X",
        ["youtube.com"] = "YouTube",
    };

    public async Task TrackPageViewAsync(string slug, TrackPageViewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VisitorId) || string.IsNullOrWhiteSpace(request.Path))
            return;

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Slug == slug && s.IsActive);
        if (store is null) return;

        db.PageViews.Add(new PageView
        {
            StoreId = store.Id,
            Path = request.Path.Length > MaxPathLength ? request.Path[..MaxPathLength] : request.Path,
            VisitorId = request.VisitorId,
            ReferrerHost = ResolveReferrerHost(request.Referrer, store),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    public async Task<StoreAnalyticsSummaryResponse> GetSummaryAsync(string clerkUserId, int days)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var since = DateTimeOffset.UtcNow.AddDays(-Math.Clamp(days, 1, 365));

        var views = db.PageViews.Where(v => v.StoreId == store.Id && v.CreatedAt >= since);

        var visits = await views.Select(v => v.VisitorId).Distinct().CountAsync();
        var pageViews = await views.CountAsync();
        var productViews = await views.CountAsync(v => v.Path.StartsWith("/products/") && !v.Path.StartsWith("/products/category/"));
        var orders = await db.Orders.CountAsync(o => o.StoreId == store.Id && o.CreatedAt >= since && o.Status != OrderStatus.Cancelled);

        // GroupBy().Select(g => new TopPageEntry(...)).OrderByDescending(p => p.Views) can't be
        // translated to SQL — EF Core can't map the record's Views property in the ORDER BY back
        // to the aggregate Count() that produced it. Materializing the group counts as an
        // anonymous type first (which EF *can* translate) and building the record + sorting +
        // limiting on the client avoids the crash. See `sources` below, which already did this.
        var topPagesRaw = await views
            .GroupBy(v => v.Path)
            .Select(g => new { Path = g.Key, Count = g.Count() })
            .ToListAsync();
        var topPages = topPagesRaw
            .Select(g => new TopPageEntry(g.Path, g.Count))
            .OrderByDescending(p => p.Views)
            .Take(TopPagesLimit)
            .ToList();

        var referrerCounts = await views
            .GroupBy(v => v.ReferrerHost)
            .Select(g => new { Host = g.Key, Count = g.Count() })
            .ToListAsync();

        var sources = referrerCounts
            .Select(g => new ReferrerEntry(
                g.Host is null ? "Direct" : KnownReferrers.GetValueOrDefault(g.Host, g.Host),
                g.Count))
            .OrderByDescending(s => s.Visits)
            .ToList();

        return new StoreAnalyticsSummaryResponse(visits, pageViews, productViews, orders, topPages, sources);
    }

    private static string? ResolveReferrerHost(string? referrer, Store store)
    {
        if (string.IsNullOrWhiteSpace(referrer) || !Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
            return null;

        var host = uri.Host.StartsWith("www.") ? uri.Host[4..] : uri.Host;

        // A visitor navigating between pages on the same store isn't a traffic "source" - the
        // browser still sets a Referrer header for same-site navigations, so without this check
        // every internal page-to-page click would get miscounted as a self-referral.
        var isSelfReferral = host.EndsWith(".nipnip.ge", StringComparison.OrdinalIgnoreCase)
            || (store.CustomDomain is not null && host.Equals(store.CustomDomain, StringComparison.OrdinalIgnoreCase));

        return isSelfReferral ? null : host;
    }
}
