using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.Flitt;

[ApiController]
[Route("api/stores/me/flitt")]
[Authorize]
public class FlittController(FlittService flitt) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectFlittRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await flitt.ConnectAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await flitt.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<FlittStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await flitt.GetStatusAsync(clerkUserId));
    }
}
