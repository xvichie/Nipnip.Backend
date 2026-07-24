using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/bundles")]
[Authorize]
public class ProductBundleController(ProductBundleService bundleService) : ControllerBase
{
    [HttpGet("/api/stores/{slug}/bundles")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<ProductBundleResponse>>> GetAllPublic(string slug)
    {
        return Ok(await bundleService.GetAllForStoreSlugAsync(slug));
    }

    [HttpGet("/api/stores/{slug}/bundles/{bundleSlug}")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ProductBundleResponse>> GetBySlugPublic(string slug, string bundleSlug)
    {
        return Ok(await bundleService.GetBySlugForStoreSlugAsync(slug, bundleSlug));
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductBundleResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await bundleService.GetAllForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<ProductBundleResponse>> Create([FromBody] CreateProductBundleRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await bundleService.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductBundleResponse>> Update(Guid id, [FromBody] UpdateProductBundleRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await bundleService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await bundleService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }
}
