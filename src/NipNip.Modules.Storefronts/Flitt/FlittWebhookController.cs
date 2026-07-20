using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NipNip.Modules.Storefronts.Flitt;

[ApiController]
[Route("api/webhooks/flitt")]
[AllowAnonymous]
public class FlittWebhookController(FlittService flitt, ILogger<FlittWebhookController> logger) : ControllerBase
{
    // Flitt requires HTTP 200 to consider a callback delivered, and otherwise retries on a
    // schedule of 2s, 60s, 300s, 600s, 3600s, 86400s — so this always returns Ok() once the
    // body at least parses as JSON, even for payloads FlittService.HandleCallbackAsync
    // ultimately ignores (unknown signature/order), matching MessengerWebhookController's
    // same "ack fast, log and drop anything unusable" convention.
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
            body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        JsonNode? payload;
        try { payload = JsonNode.Parse(body); }
        catch (JsonException)
        {
            logger.LogWarning("Flitt callback with unparseable body.");
            return Ok();
        }

        if (payload is null) return Ok();

        await flitt.HandleCallbackAsync(payload);
        return Ok();
    }
}
