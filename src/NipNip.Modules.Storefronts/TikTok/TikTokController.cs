using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.TikTok;

[ApiController]
[Route("api/stores/me/tiktok")]
[Authorize]
public class TikTokController(TikTokConnectionService tiktok) : ControllerBase
{
    [HttpGet("connect-url")]
    public async Task<ActionResult<TikTokConnectUrlResponse>> GetConnectUrl()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(new TikTokConnectUrlResponse(await tiktok.BuildConnectUrlAsync(clerkUserId)));
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state)
    {
        return Redirect(await tiktok.HandleCallbackAsync(code, state));
    }

    [HttpGet("status")]
    public async Task<ActionResult<TikTokStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await tiktok.GetStatusAsync(clerkUserId));
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await tiktok.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("publish/{productId:guid}/preview")]
    public async Task<ActionResult<TikTokProductPreviewResponse>> PreviewProduct(Guid productId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await tiktok.PreviewProductPostAsync(clerkUserId, productId));
    }

    [HttpPost("publish/{productId:guid}")]
    public async Task<ActionResult<TikTokPublishResponse>> PublishProduct(Guid productId, [FromBody] TikTokPublishRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await tiktok.PublishProductAsync(clerkUserId, productId, request));
    }

    [HttpPost("publish-images")]
    public async Task<ActionResult<TikTokPublishResponse>> PublishImages([FromBody] TikTokPublishImagesRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await tiktok.PublishImagesAsync(clerkUserId, request));
    }
}
