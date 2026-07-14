using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/pages")]
[Authorize]
public class StorePageController(StorePageService storePageService) : ControllerBase
{
    [HttpGet("/api/stores/{slug}/pages")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<StorePageResponse>>> GetAllPublic(string slug)
    {
        return Ok(await storePageService.GetAllForStoreSlugAsync(slug));
    }

    [HttpGet("/api/stores/{slug}/pages/{pageSlug}")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<StorePageResponse>> GetBySlugPublic(string slug, string pageSlug)
    {
        return Ok(await storePageService.GetBySlugForStoreSlugAsync(slug, pageSlug));
    }

    [HttpGet]
    public async Task<ActionResult<List<StorePageResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storePageService.GetAllForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<StorePageResponse>> Create([FromBody] CreateStorePageRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storePageService.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StorePageResponse>> Update(Guid id, [FromBody] UpdateStorePageRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await storePageService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await storePageService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }
}
