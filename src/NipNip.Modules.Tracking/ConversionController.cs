using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Tracking.DTOs;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Tracking;

[ApiController]
[Route("api/conversions")]
[Authorize]
public class ConversionController(TrackingService trackingService) : ControllerBase
{
    [HttpGet("merchant")]
    public async Task<ActionResult<PaginatedResult<MerchantConversionEntry>>> GetMerchantConversions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.GetMerchantConversionsAsync(clerkUserId, page, pageSize, month, year));
    }

    [HttpGet("merchant/monthly")]
    public async Task<ActionResult<List<MonthlyMerchantSummary>>> GetMerchantMonthlySummary()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.GetMerchantMonthlySummaryAsync(clerkUserId));
    }

    [HttpGet("me")]
    public async Task<ActionResult<PaginatedResult<CreatorEarningEntry>>> GetMyConversions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? month = null,
        [FromQuery] int? year = null)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.GetMyConversionsAsync(clerkUserId, page, pageSize, month, year));
    }

    [HttpGet("me/monthly")]
    public async Task<ActionResult<List<MonthlyCreatorSummary>>> GetMyMonthlySummary()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.GetMyMonthlySummaryAsync(clerkUserId));
    }

    [HttpPost("track")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ConversionResponse>> Track([FromBody] TrackConversionRequest request)
    {
        var apiKey = Request.Headers["X-Merchant-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized();

        return Ok(await trackingService.TrackConversionAsync(apiKey, request));
    }

    [HttpPost("manual")]
    public async Task<ActionResult<ConversionResponse>> Manual([FromBody] ManualConversionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.ManualConversionAsync(clerkUserId, request));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminConversionEntry>> GetById(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await trackingService.GetConversionByIdAsync(clerkUserId, id));
    }
}
