using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/categories")]
[Authorize]
public class CategoryController(CategoryService categoryService) : ControllerBase
{
    [HttpGet("/api/stores/{slug}/categories")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<List<CategoryResponse>>> GetAllPublic(string slug)
    {
        return Ok(await categoryService.GetAllForStoreSlugAsync(slug));
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await categoryService.GetAllForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CreateCategoryRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await categoryService.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await categoryService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await categoryService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }
}
