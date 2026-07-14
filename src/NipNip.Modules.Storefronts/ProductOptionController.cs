using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/products/{productId:guid}/options")]
[Authorize]
public class ProductOptionController(ProductOptionService optionService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProductOptionResponse>> CreateOption(Guid productId, [FromBody] CreateProductOptionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await optionService.CreateOptionAsync(clerkUserId, productId, request));
    }

    [HttpDelete("{optionId:guid}")]
    public async Task<IActionResult> DeleteOption(Guid productId, Guid optionId)
    {
        var clerkUserId = User.GetClerkUserId();
        await optionService.DeleteOptionAsync(clerkUserId, productId, optionId);
        return NoContent();
    }

    [HttpPost("{optionId:guid}/values")]
    public async Task<ActionResult<ProductOptionValueResponse>> CreateValue(
        Guid productId, Guid optionId, [FromBody] CreateProductOptionValueRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await optionService.CreateValueAsync(clerkUserId, productId, optionId, request));
    }

    [HttpDelete("{optionId:guid}/values/{valueId:guid}")]
    public async Task<IActionResult> DeleteValue(Guid productId, Guid optionId, Guid valueId)
    {
        var clerkUserId = User.GetClerkUserId();
        await optionService.DeleteValueAsync(clerkUserId, productId, optionId, valueId);
        return NoContent();
    }
}
