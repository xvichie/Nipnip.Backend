using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NipNip.Modules.Storefronts.Bog;

[ApiController]
[Route("api/webhooks/bog")]
[AllowAnonymous]
public class BogWebhookController(BogService bog, ILogger<BogWebhookController> logger) : ControllerBase
{
    // The callback body nests everything under "body": { "pre_order_id": "...", ... }. We don't
    // hold BOG's public key to verify the Callback-Signature header, so (like TbcWebhookController)
    // this only extracts the id and treats the callback as a trigger to re-pull the real status
    // ourselves — see BogService.HandleCallbackAsync. Always returns Ok() once the body at least
    // parses as JSON, matching the other webhook controllers' "ack fast" convention.
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
            logger.LogWarning("BOG callback with unparseable body.");
            return Ok();
        }

        var preOrderId = payload?["body"]?["pre_order_id"]?.GetValue<string>();
        if (preOrderId is null)
        {
            logger.LogWarning("BOG callback missing body.pre_order_id.");
            return Ok();
        }

        await bog.HandleCallbackAsync(preOrderId);
        return Ok();
    }
}
