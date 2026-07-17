using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/facebook")]
[Authorize]
public class FacebookController(FacebookConnectionService facebook) : ControllerBase
{
    [HttpGet("connect-url")]
    public async Task<ActionResult<FacebookConnectUrlResponse>> GetConnectUrl()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(new FacebookConnectUrlResponse(await facebook.BuildConnectUrlAsync(clerkUserId)));
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? state)
    {
        return Redirect(await facebook.HandleCallbackAsync(code, state));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<FacebookPendingPageResponse>>> GetPending([FromQuery] string token)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await facebook.GetPendingChoicesAsync(clerkUserId, token));
    }

    [HttpPost("select-page")]
    public async Task<IActionResult> SelectPage([FromBody] SelectFacebookPageRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await facebook.SelectPageAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<FacebookStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await facebook.GetStatusAsync(clerkUserId));
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await facebook.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("posts")]
    public async Task<ActionResult<List<FacebookPostSummaryResponse>>> GetPosts()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await facebook.GetPostsAsync(clerkUserId));
    }

    [HttpGet("posts/{postId}")]
    public async Task<ActionResult<FacebookPostDetailResponse>> GetPostDetail(string postId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await facebook.GetPostDetailAsync(clerkUserId, postId));
    }

    [HttpGet("publish/{productId:guid}/preview")]
    public async Task<ActionResult<FacebookProductPreviewResponse>> PreviewProduct(Guid productId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await facebook.PreviewProductPostAsync(clerkUserId, productId));
    }

    [HttpPost("publish/{productId:guid}")]
    public async Task<ActionResult<FacebookPublishResponse>> PublishProduct(Guid productId, [FromBody] FacebookPublishRequest? request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(new FacebookPublishResponse(await facebook.PublishProductAsync(clerkUserId, productId, request?.Message)));
    }
}
