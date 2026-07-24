using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/{slug}/cart")]
[AllowAnonymous]
[EnableCors("Public")]
public class CartController(CartService cartService) : ControllerBase
{
    private const string SessionHeader = "X-Cart-Session";

    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart(string slug)
    {
        return Ok(await cartService.GetCartAsync(slug, Request.Headers[SessionHeader]));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddItem(string slug, [FromBody] AddCartItemRequest request)
    {
        return Ok(await cartService.AddItemAsync(slug, Request.Headers[SessionHeader], request));
    }

    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> UpdateItem(string slug, Guid itemId, [FromBody] UpdateCartItemRequest request)
    {
        return Ok(await cartService.UpdateItemAsync(slug, Request.Headers[SessionHeader], itemId, request));
    }

    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> RemoveItem(string slug, Guid itemId)
    {
        return Ok(await cartService.RemoveItemAsync(slug, Request.Headers[SessionHeader], itemId));
    }

    [HttpPost("bundles")]
    public async Task<ActionResult<CartResponse>> AddBundle(string slug, [FromBody] AddBundleToCartRequest request)
    {
        return Ok(await cartService.AddBundleToCartAsync(slug, Request.Headers[SessionHeader], request));
    }

    [HttpPut("bundles/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> UpdateBundle(string slug, Guid itemId, [FromBody] UpdateCartBundleItemRequest request)
    {
        return Ok(await cartService.UpdateBundleItemAsync(slug, Request.Headers[SessionHeader], itemId, request));
    }

    [HttpDelete("bundles/{itemId:guid}")]
    public async Task<ActionResult<CartResponse>> RemoveBundle(string slug, Guid itemId)
    {
        return Ok(await cartService.RemoveBundleItemAsync(slug, Request.Headers[SessionHeader], itemId));
    }
}
