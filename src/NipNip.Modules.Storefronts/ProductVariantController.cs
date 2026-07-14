using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/products/{productId:guid}/variants")]
[Authorize]
public class ProductVariantController(ProductVariantService variantService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductVariantResponse>> Create(Guid productId, [FromBody] CreateProductVariantRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await variantService.CreateAsync(clerkUserId, productId, request));
    }

    [HttpPut("{variantId:guid}")]
    public async Task<ActionResult<ProductVariantResponse>> Update(
        Guid productId, Guid variantId, [FromBody] UpdateProductVariantRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await variantService.UpdateAsync(clerkUserId, productId, variantId, request));
    }

    [HttpDelete("{variantId:guid}")]
    public async Task<IActionResult> Delete(Guid productId, Guid variantId)
    {
        var clerkUserId = User.GetClerkUserId();
        await variantService.DeleteAsync(clerkUserId, productId, variantId);
        return NoContent();
    }
}
