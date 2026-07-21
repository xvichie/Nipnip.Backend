namespace NipNip.Modules.Storefronts.DTOs;

public record TrackPageViewRequest(string Path, string? Referrer, string VisitorId);

public record TopPageEntry(string Path, int Views);

public record ReferrerEntry(string Source, int Visits);

// A simple 3-stage funnel: Visits (unique visitors) -> ProductViews (visits to a product page,
// a proxy for buying interest) -> Orders (completed purchases). Deeper stages (add-to-cart,
// checkout-started) aren't tracked yet - this is deliberately the funnel buildable from data
// that already exists (Orders) plus the one new thing being added (PageView), not a promise to
// instrument every interaction on the storefront.
public record StoreAnalyticsSummaryResponse(
    int Visits,
    int PageViews,
    int ProductViews,
    int Orders,
    List<TopPageEntry> TopPages,
    List<ReferrerEntry> Sources
);
