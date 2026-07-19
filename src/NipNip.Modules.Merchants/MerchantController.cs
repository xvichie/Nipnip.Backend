using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Merchants.DTOs;
using NipNip.Modules.Merchants.Extensions;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Merchants;

[ApiController]
[Route("api/merchants")]
[Authorize]
public class MerchantController(MerchantService merchantService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedResult<MerchantResponse>>> GetAll([FromQuery] PaginatedRequest pagination)
    {
        return Ok(await merchantService.GetAllAsync(pagination, User.TryGetClerkUserId()));
    }

    [HttpGet("highlighted")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MerchantResponse>>> GetHighlighted()
    {
        return Ok(await merchantService.GetHighlightedAsync(User.TryGetClerkUserId()));
    }

    [HttpGet("me")]
    public async Task<ActionResult<MerchantResponse>> GetMe()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await merchantService.GetMeAsync(clerkUserId));
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<MerchantResponse>> GetBySlug(string slug)
    {
        return Ok(await merchantService.GetBySlugAsync(slug, User.TryGetClerkUserId()));
    }

    [HttpPost]
    public async Task<ActionResult<MerchantResponse>> Register([FromBody] RegisterMerchantRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        var merchant = await merchantService.RegisterAsync(clerkUserId, request);
        return CreatedAtAction(nameof(GetBySlug), new { slug = merchant.Slug }, merchant);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MerchantResponse>> Update(Guid id, [FromBody] UpdateMerchantRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await merchantService.UpdateAsync(id, clerkUserId, request));
    }

    [HttpGet("me/snippet")]
    public async Task<ActionResult<MerchantSnippetResponse>> GetSnippet()
    {
        var clerkUserId = User.GetClerkUserId();
        var apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(await merchantService.GetSnippetAsync(clerkUserId, apiBaseUrl));
    }

    [HttpGet("me/dashboard")]
    public async Task<ActionResult<MerchantDashboardResponse>> GetDashboard(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await merchantService.GetDashboardAsync(clerkUserId, from, to));
    }

    [HttpGet("me/approved-creators")]
    public async Task<ActionResult<List<ApprovedCreatorResponse>>> GetApprovedCreators()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await merchantService.GetApprovedCreatorsAsync(clerkUserId));
    }

    [HttpPost("me/approved-creators")]
    public async Task<ActionResult<ApprovedCreatorResponse>> AddApprovedCreator([FromBody] AddApprovedCreatorRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await merchantService.AddApprovedCreatorAsync(clerkUserId, request));
    }

    [HttpDelete("me/approved-creators/{creatorId:guid}")]
    public async Task<ActionResult> RemoveApprovedCreator(Guid creatorId)
    {
        var clerkUserId = User.GetClerkUserId();
        await merchantService.RemoveApprovedCreatorAsync(clerkUserId, creatorId);
        return NoContent();
    }
}
