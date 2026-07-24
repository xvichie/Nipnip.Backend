using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/collections")]
[Authorize]
public class CollectionController(CollectionService collectionService) : ControllerBase
{
    [HttpGet("/api/stores/{slug}/collections")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<CollectionResponse>>> GetAllPublic(string slug)
    {
        return Ok(await collectionService.GetAllForStoreSlugAsync(slug));
    }

    [HttpGet]
    public async Task<ActionResult<List<CollectionResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await collectionService.GetAllForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<CollectionResponse>> Create([FromBody] CreateCollectionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await collectionService.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CollectionResponse>> Update(Guid id, [FromBody] UpdateCollectionRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await collectionService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await collectionService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }

    [HttpGet("{id:guid}/products")]
    public async Task<ActionResult<List<ProductSummaryResponse>>> GetProducts(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await collectionService.GetProductsAsync(clerkUserId, id));
    }

    [HttpPut("{id:guid}/products")]
    public async Task<ActionResult<List<ProductSummaryResponse>>> SetProducts(Guid id, [FromBody] SetCollectionProductsRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await collectionService.SetProductsAsync(clerkUserId, id, request));
    }
}
