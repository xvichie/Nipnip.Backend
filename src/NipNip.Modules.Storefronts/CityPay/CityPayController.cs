using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.CityPay;

[ApiController]
[Route("api/stores/me/citypay")]
[Authorize]
public class CityPayController(CityPayService cityPay) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectCityPayRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await cityPay.ConnectAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await cityPay.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<CityPayStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await cityPay.GetStatusAsync(clerkUserId));
    }
}
