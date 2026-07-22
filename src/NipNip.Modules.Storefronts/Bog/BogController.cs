using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.Bog;

[ApiController]
[Route("api/stores/me/bog")]
[Authorize]
public class BogController(BogService bog) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectBogRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await bog.ConnectAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await bog.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<BogStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await bog.GetStatusAsync(clerkUserId));
    }
}
