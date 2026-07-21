using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores")]
[Authorize]
public class StoreController(StoreService storeService, StoreAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<StoreResponse>> GetBySlug(string slug)
    {
        return Ok(await storeService.GetBySlugAsync(slug));
    }

    [HttpGet("sitemap")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<StoreSitemapEntryResponse>>> GetSitemapSlugs()
    {
        return Ok(await storeService.GetSitemapSlugsAsync());
    }

    [HttpPost("{slug}/track-pageview")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<IActionResult> TrackPageView(string slug, [FromBody] TrackPageViewRequest request)
    {
        await analyticsService.TrackPageViewAsync(slug, request);
        return NoContent();
    }

    [HttpGet("me/analytics")]
    public async Task<ActionResult<StoreAnalyticsSummaryResponse>> GetAnalytics([FromQuery] int days = 30)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await analyticsService.GetSummaryAsync(clerkUserId, days));
    }

    [HttpPost]
    public async Task<ActionResult<StoreResponse>> Create([FromBody] CreateStoreRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        var store = await storeService.CreateAsync(clerkUserId, request);
        return CreatedAtAction(nameof(GetMe), store);
    }

    [HttpGet("me")]
    public async Task<ActionResult<StoreResponse>> GetMe()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storeService.GetMeAsync(clerkUserId));
    }

    [HttpPut("me")]
    public async Task<ActionResult<StoreResponse>> UpdateMe([FromBody] UpdateStoreRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storeService.UpdateMeAsync(clerkUserId, request));
    }

    [HttpGet("me/domain")]
    public async Task<ActionResult<StoreDomainResponse>> GetDomain()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storeService.GetDomainStatusAsync(clerkUserId));
    }

    [HttpPut("me/domain")]
    public async Task<ActionResult<StoreDomainResponse>> SetDomain([FromBody] SetStoreDomainRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storeService.SetDomainAsync(clerkUserId, request));
    }

    [HttpDelete("me/domain")]
    public async Task<IActionResult> DeleteDomain()
    {
        var clerkUserId = User.GetClerkUserId();
        await storeService.RemoveDomainAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("by-domain/{domain}")]
    [AllowAnonymous]
    public async Task<ActionResult<StoreSlugResponse>> ResolveByDomain(string domain)
    {
        var slug = await storeService.ResolveSlugByDomainAsync(domain);
        return slug is null ? NotFound() : Ok(new StoreSlugResponse(slug));
    }
}
