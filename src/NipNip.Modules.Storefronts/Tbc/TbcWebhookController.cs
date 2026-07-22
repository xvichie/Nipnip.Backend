using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NipNip.Modules.Storefronts.Tbc;

[ApiController]
[Route("api/webhooks/tbc")]
[AllowAnonymous]
public class TbcWebhookController(TbcService tbc, ILogger<TbcWebhookController> logger) : ControllerBase
{
    // TBC's callback body is just {"PaymentId":"..."} — a notify-then-pull design where the
    // real status is only trustworthy via GET /payments/{payId}, so unlike Flitt there's no
    // signature to verify here. Always returns Ok() once the body at least parses as JSON,
    // matching FlittWebhookController/MessengerWebhookController's "ack fast" convention.
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
            logger.LogWarning("TBC callback with unparseable body.");
            return Ok();
        }

        var paymentId = payload?["PaymentId"]?.GetValue<string>() ?? payload?["paymentId"]?.GetValue<string>();
        if (paymentId is null)
        {
            logger.LogWarning("TBC callback missing PaymentId.");
            return Ok();
        }

        await tbc.HandleCallbackAsync(paymentId);
        return Ok();
    }
}
