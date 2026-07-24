using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/me/discount-codes")]
[Authorize]
public class StoreDiscountCodeController(StoreDiscountCodeService discountCodeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<StoreDiscountCodeResponse>>> GetAll()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await discountCodeService.GetAllForOwnStoreAsync(clerkUserId));
    }

    [HttpPost]
    public async Task<ActionResult<StoreDiscountCodeResponse>> Create([FromBody] CreateStoreDiscountCodeRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await discountCodeService.CreateAsync(clerkUserId, request));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StoreDiscountCodeResponse>> Update(Guid id, [FromBody] UpdateStoreDiscountCodeRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await discountCodeService.UpdateAsync(clerkUserId, id, request));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var clerkUserId = User.GetClerkUserId();
        await discountCodeService.DeleteAsync(clerkUserId, id);
        return NoContent();
    }

    [HttpPost("/api/stores/{slug}/discount-codes/validate")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<ValidateDiscountCodeResponse>> Validate(string slug, [FromBody] ValidateDiscountCodeRequest request)
    {
        return Ok(await discountCodeService.ValidateAsync(slug, request));
    }
}
