using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Extensions;

namespace NipNip.Modules.Storefronts.AiAgent;

[ApiController]
[Route("api/stores/me/ai-agent")]
[Authorize]
public class AiAgentController(AiAgentSettingsService settings, ConversationService conversations, StoreService storeService, AiAgentOrchestrator orchestrator) : ControllerBase
{
    [HttpGet("settings")]
    public async Task<ActionResult<AiAgentSettingsResponse>> GetSettings()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await settings.GetAsync(clerkUserId));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<AiAgentSettingsResponse>> UpdateSettings([FromBody] UpdateAiAgentSettingsRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await settings.UpdateAsync(clerkUserId, request));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationSummaryResponse>>> GetConversations()
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await conversations.ListForOwnStoreAsync(clerkUserId));
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<ConversationDetailResponse>> GetConversation(Guid conversationId)
    {
        var clerkUserId = User.GetClerkUserId();
        return Ok(await conversations.GetDetailForOwnStoreAsync(clerkUserId, conversationId));
    }

    // Temporary — lets a merchant exercise the full reasoning + tool-selection + reply
    // loop from Swagger/Postman with a synthetic PSID, with zero Facebook involvement.
    // Remove once the real Messenger webhook (step 8) is live-tested.
    [HttpPost("debug-message")]
    public async Task<ActionResult<AiAgentDebugMessageResponse>> DebugMessage([FromBody] AiAgentDebugMessageRequest request)
    {
        var clerkUserId = User.GetClerkUserId();
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var psid = string.IsNullOrWhiteSpace(request.Psid) ? "debug-user" : request.Psid.Trim();
        var conversation = await conversations.GetOrCreateConversationAsync(store.Id, psid, "Debug User");
        await conversations.AppendMessageAsync(conversation.Id, ConversationMessageDirection.Inbound, request.Text, null);

        var reply = await orchestrator.RunTurnAsync(store.Id, conversation.Id);
        return Ok(new AiAgentDebugMessageResponse(reply));
    }
}
