using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Creators.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Creators;

[ApiController]
[Route("api/linktrees")]
[Authorize]
public class LinkTreeController(LinkTreeService linkTreeService) : ControllerBase
{
    [HttpGet("by-creator/{creatorSlug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicLinkTreeResponse>> GetDefaultByCreatorSlug(string creatorSlug)
    {
        return Ok(await linkTreeService.GetDefaultByCreatorSlugAsync(creatorSlug));
    }

    [HttpGet("me")]
    public async Task<ActionResult<List<LinkTreeSummaryResponse>>> GetMyTrees()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.GetMyTreesAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<LinkTreeSummaryResponse>> CreateTree([FromBody] CreateLinkTreeRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.CreateTreeAsync(clerkUserId, request));
    }

    [HttpGet("me/{id:guid}")]
    public async Task<ActionResult<LinkTreeDetailResponse>> GetMyTreeDetail(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.GetMyTreeDetailAsync(clerkUserId, id));
    }

    [HttpPut("me/{id:guid}")]
    public async Task<ActionResult<LinkTreeSummaryResponse>> UpdateTree(Guid id, [FromBody] UpdateLinkTreeRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.UpdateTreeAsync(clerkUserId, id, request));
    }

    [HttpDelete("me/{id:guid}")]
    public async Task<ActionResult> DeleteTree(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await linkTreeService.DeleteTreeAsync(clerkUserId, id);
        return NoContent();
    }

    [HttpPut("me/{id:guid}/set-default")]
    public async Task<ActionResult> SetDefault(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await linkTreeService.SetDefaultAsync(clerkUserId, id);
        return NoContent();
    }

    [HttpPost("me/{id:guid}/items")]
    public async Task<ActionResult<LinkTreeItemResponse>> AddItem(Guid id, [FromBody] AddLinkTreeItemRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.AddItemAsync(clerkUserId, id, request));
    }

    [HttpPut("me/items/{itemId:guid}")]
    public async Task<ActionResult<LinkTreeItemResponse>> UpdateItem(Guid itemId, [FromBody] UpdateLinkTreeItemRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await linkTreeService.UpdateItemAsync(clerkUserId, itemId, request));
    }

    [HttpDelete("me/items/{itemId:guid}")]
    public async Task<ActionResult> DeleteItem(Guid itemId)
    {
        var clerkUserId = User.GetClerkUserId();
        await linkTreeService.RemoveItemAsync(clerkUserId, itemId);
        return NoContent();
    }

    [HttpPut("me/{id:guid}/reorder")]
    public async Task<ActionResult> Reorder(Guid id, [FromBody] ReorderLinkTreeItemsRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await linkTreeService.ReorderAsync(clerkUserId, id, request);
        return NoContent();
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicLinkTreeResponse>> GetByTreeSlug(string slug)
    {
        return Ok(await linkTreeService.GetByTreeSlugAsync(slug));
    }
}
