using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.MyMarket;

[ApiController]
[Route("api/stores/me/mymarket")]
[Authorize]
public class MyMarketController(MyMarketService myMarket) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectMyMarketRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await myMarket.ConnectAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await myMarket.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<MyMarketStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await myMarket.GetStatusAsync(clerkUserId));
    }
}
