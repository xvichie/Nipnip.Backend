using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NipNip.Modules.Tracking.DTOs;

namespace NipNip.Modules.Tracking;

[ApiController]
[Route("api/tracking")]
public class TrackingController(TrackingService trackingService) : ControllerBase
{
    [HttpPost("ping")]
    [AllowAnonymous]
    [EnableCors("Public")]
    public async Task<ActionResult<PingResponse>> Ping()
    {
        var apiKey = Request.Headers["X-Merchant-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(apiKey))
            return Unauthorized();

        var name = await trackingService.VerifyApiKeyAsync(apiKey);
        return Ok(new PingResponse(name));
    }
}
