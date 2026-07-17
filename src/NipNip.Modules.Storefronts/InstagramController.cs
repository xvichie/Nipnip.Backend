using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/instagram")]
[Authorize]
public class InstagramController(InstagramConnectionService instagram) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<InstagramStatusResponse>> GetStatus()
    {
        return Ok(await instagram.GetStatusAsync(User.GetClerkUserId()));
    }

    [HttpGet("media")]
    public async Task<ActionResult<List<InstagramMediaSummaryResponse>>> GetMedia()
    {
        return Ok(await instagram.GetMediaAsync(User.GetClerkUserId()));
    }

    [HttpGet("media/{mediaId}")]
    public async Task<ActionResult<InstagramMediaDetailResponse>> GetMediaDetail(string mediaId)
    {
        return Ok(await instagram.GetMediaDetailAsync(User.GetClerkUserId(), mediaId));
    }

    [HttpGet("publish/{productId:guid}/preview")]
    public async Task<ActionResult<InstagramProductPreviewResponse>> PreviewProduct(Guid productId)
    {
        return Ok(await instagram.PreviewProductPostAsync(User.GetClerkUserId(), productId));
    }

    [HttpPost("publish/{productId:guid}")]
    public async Task<ActionResult<InstagramPublishResponse>> PublishProduct(Guid productId, [FromBody] InstagramPublishRequest? request)
    {
        return Ok(new InstagramPublishResponse(await instagram.PublishProductAsync(User.GetClerkUserId(), productId, request?.Message)));
    }
}
