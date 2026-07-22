using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NipNip.Modules.Storefronts.CityPay;

[ApiController]
[Route("api/webhooks/citypay")]
[AllowAnonymous]
public class CityPayWebhookController(CityPayService cityPay, ILogger<CityPayWebhookController> logger) : ControllerBase
{
    // CityPay's callback carries no signature — we don't hold anything to verify it with, so
    // (like TbcWebhookController/BogWebhookController) this only extracts order_token and
    // treats the callback as a trigger to re-pull the real status ourselves; see
    // CityPayService.HandleCallbackAsync. Always returns Ok() once the body at least parses as
    // JSON, matching the other webhook controllers' "ack fast" convention.
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
            logger.LogWarning("CityPay callback with unparseable body.");
            return Ok();
        }

        var orderToken = payload?["order_token"]?.GetValue<string>();
        if (orderToken is null)
        {
            logger.LogWarning("CityPay callback missing order_token.");
            return Ok();
        }

        await cityPay.HandleCallbackAsync(orderToken);
        return Ok();
    }
}
