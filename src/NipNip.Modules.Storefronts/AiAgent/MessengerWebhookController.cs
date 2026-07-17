using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NipNip.Data;
using NipNip.Data.Enums;
using NipNip.Shared.Crypto;

namespace NipNip.Modules.Storefronts.AiAgent;

[ApiController]
[Route("api/webhooks/facebook")]
[AllowAnonymous]
public class MessengerWebhookController(
    IOptions<FacebookOptions> options,
    AppDbContext db,
    ConversationService conversations,
    IServiceScopeFactory scopeFactory,
    ILogger<MessengerWebhookController> logger) : ControllerBase
{
    private readonly FacebookOptions _options = options.Value;

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        if (mode == "subscribe" && challenge is not null && verifyToken == _options.MessengerVerifyToken)
            return Content(challenge, "text/plain");

        return Forbid();
    }

    // Always returns 200 once the signature checks out, even for payloads it ignores —
    // Meta retries aggressively on anything else, and there's nothing to retry here.
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        Request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
            body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        if (!IsValidSignature(body, Request.Headers["X-Hub-Signature-256"].ToString()))
            return Unauthorized();

        JsonNode? payload;
        try { payload = JsonNode.Parse(body); }
        catch (JsonException) { return Ok(); }

        if (payload?["object"]?.GetValue<string>() != "page" || payload["entry"] is not JsonArray entries)
            return Ok();

        foreach (var entry in entries)
        {
            var pageId = entry?["id"]?.GetValue<string>();
            if (pageId is null || entry?["messaging"] is not JsonArray messagingEvents) continue;

            foreach (var evt in messagingEvents)
            {
                var senderId = evt?["sender"]?["id"]?.GetValue<string>();
                var text = evt?["message"]?["text"]?.GetValue<string>();
                var mid = evt?["message"]?["mid"]?.GetValue<string>();
                if (senderId is null || text is null) continue;

                await HandleIncomingMessageAsync(pageId, senderId, text, mid);
            }
        }

        return Ok();
    }

    private async Task HandleIncomingMessageAsync(string pageId, string senderId, string text, string? mid)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.FacebookPageId == pageId);
        if (store is null || !store.AiAgentEnabledFacebook) return;

        // Meta redelivers webhooks at least once — skip anything we've already recorded.
        if (mid is not null && await conversations.ExternalMessageExistsAsync(mid)) return;

        var conversation = await conversations.GetOrCreateConversationAsync(store.Id, senderId, null);
        await conversations.AppendMessageAsync(conversation.Id, ConversationMessageDirection.Inbound, text, mid);

        var storeId = store.Id;
        var conversationId = conversation.Id;
        var pageAccessTokenEncrypted = store.FacebookPageAccessTokenEncrypted;

        // Respond to Meta immediately; the AI turn + reply happen after, in their own
        // DI scope, mirroring RedirectController.TrackAndRedirect's fire-and-forget
        // click-logging pattern. Same durability trade-off: a process restart mid-flight
        // drops the reply — acceptable for v1, revisit only if it becomes a real problem.
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<AiAgentOrchestrator>();
                var reply = await orchestrator.RunTurnAsync(storeId, conversationId);

                if (pageAccessTokenEncrypted is not null)
                {
                    var graph = scope.ServiceProvider.GetRequiredService<GraphApiClient>();
                    var protector = scope.ServiceProvider.GetRequiredService<AesStringProtector>();
                    await graph.SendMessengerMessageAsync(protector.Decrypt(pageAccessTokenEncrypted), senderId, reply);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process AI agent reply for conversation {ConversationId}", conversationId);
            }
        });
    }

    private bool IsValidSignature(string body, string signatureHeader)
    {
        if (!signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            var expected = Convert.FromHexString(signatureHeader["sha256=".Length..]);
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.AppSecret));
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
            return CryptographicOperations.FixedTimeEquals(computed, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
