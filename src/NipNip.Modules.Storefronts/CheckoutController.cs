using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;

namespace NipNip.Modules.Storefronts;

[ApiController]
[Route("api/stores/{slug}/checkout")]
[AllowAnonymous]
[EnableCors("Public")]
public class CheckoutController(CartService cartService) : ControllerBase
{
    private const string SessionHeader = "X-Cart-Session";

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Checkout(string slug, [FromBody] CheckoutRequest request)
    {
        return Ok(await cartService.CheckoutAsync(slug, Request.Headers[SessionHeader], request));
    }

    // Public by design (unauthenticated, like the checkout POST above) — an order id is an
    // unguessable GUID, and this is the same data the browser would already have seen on an
    // inline success page; it just needs to be re-fetchable after Flitt's hosted-checkout
    // redirect takes the browser away and back via a full page navigation.
    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<OrderResponse>> GetOrderStatus(string slug, Guid orderId)
    {
        return Ok(await cartService.GetOrderStatusAsync(slug, orderId));
    }
}
