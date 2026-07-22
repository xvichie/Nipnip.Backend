using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.Tbc;

[ApiController]
[Route("api/stores/me/tbc")]
[Authorize]
public class TbcController(TbcService tbc) : ControllerBase
{
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectTbcRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        await tbc.ConnectAsync(clerkUserId, request);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Disconnect()
    {
        var clerkUserId = User.GetClerkUserId();
        await tbc.DisconnectAsync(clerkUserId);
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<ActionResult<TbcStatusResponse>> GetStatus()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await tbc.GetStatusAsync(clerkUserId));
    }
}
