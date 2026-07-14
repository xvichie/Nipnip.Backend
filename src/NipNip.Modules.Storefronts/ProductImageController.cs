using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/products/{productId:guid}/images")]
[Authorize]
public class ProductImageController(ProductImageService imageService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductImageResponse>> Create(Guid productId, [FromBody] CreateProductImageRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await imageService.CreateAsync(clerkUserId, productId, request));
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid imageId)
    {
        var clerkUserId = User.GetClerkUserId();
        await imageService.DeleteAsync(clerkUserId, productId, imageId);
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(Guid productId, [FromBody] ReorderProductImagesRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await imageService.ReorderAsync(clerkUserId, productId, request.ImageIds);
        return NoContent();
    }
}
