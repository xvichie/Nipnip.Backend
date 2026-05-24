using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Creators.DTOs;
using NipNip.Modules.Creators.Extensions;
using NipNip.Shared.Extensions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Creators;

[ApiController]
[Route("api/creators")]
[Authorize]
public class CreatorController(CreatorService creatorService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedResult<CreatorResponse>>> GetAll([FromQuery] PaginatedRequest pagination)
    {
        return Ok(await creatorService.GetAllPublicAsync(pagination));
    }

    [HttpGet("highlighted")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CreatorResponse>>> GetHighlighted()
    {
        return Ok(await creatorService.GetHighlightedAsync());
    }

    [HttpGet("me")]
    public async Task<ActionResult<CreatorResponse>> GetMe()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await creatorService.GetMeAsync(clerkUserId));
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<CreatorResponse>> GetBySlug(string slug)
    {
        return Ok(await creatorService.GetBySlugAsync(slug));
    }

    [HttpPost]
    public async Task<ActionResult<CreatorResponse>> Register([FromBody] RegisterCreatorRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        var creator = await creatorService.RegisterAsync(clerkUserId, request);
        return CreatedAtAction(nameof(GetBySlug), new { slug = creator.Slug }, creator);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CreatorResponse>> Update(Guid id, [FromBody] UpdateCreatorRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await creatorService.UpdateAsync(id, clerkUserId, request));
    }

    [HttpGet("me/dashboard")]
    public async Task<ActionResult<CreatorDashboardResponse>> GetDashboard(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await creatorService.GetDashboardAsync(clerkUserId, from, to));
    }
}
