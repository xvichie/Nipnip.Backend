using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/products/{productId:guid}/related")]
[Authorize]
public class RelatedProductController(RelatedProductService relatedProductService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductSummaryResponse>>> GetRelated(Guid productId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await relatedProductService.GetForOwnProductAsync(clerkUserId, productId));
    }

    [HttpPut]
    public async Task<ActionResult<List<ProductSummaryResponse>>> SetRelated(Guid productId, [FromBody] SetRelatedProductsRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await relatedProductService.SetForOwnProductAsync(clerkUserId, productId, request));
    }
}
