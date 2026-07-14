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
}
