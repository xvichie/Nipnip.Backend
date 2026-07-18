using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Shared.Exceptions;

namespace NipNip.Modules.Storefronts.AiAgent;

public class ConversationService(AppDbContext db, StoreService storeService)
{
    public async Task<bool> ExternalMessageExistsAsync(string externalMessageId) =>
        await db.ConversationMessages.AnyAsync(m => m.ExternalMessageId == externalMessageId);

    public async Task<Conversation> GetOrCreateConversationAsync(Guid storeId, string externalUserId, string? displayName)
    {
        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c => c.StoreId == storeId && c.ExternalUserId == externalUserId);

        if (conversation is not null)
        {
            if (displayName is not null && conversation.CustomerDisplayName != displayName)
                conversation.CustomerDisplayName = displayName;
            return conversation;
        }

        conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ExternalUserId = externalUserId,
            CustomerDisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            LastMessageAt = DateTimeOffset.UtcNow,
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation;
    }

    public async Task<ConversationMessage> AppendMessageAsync(Guid conversationId, ConversationMessageDirection direction, string content, string? externalMessageId)
    {
        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Direction = direction,
            Content = content,
            CreatedAt = DateTimeOffset.UtcNow,
            ExternalMessageId = externalMessageId,
        };
        db.ConversationMessages.Add(message);

        var conversation = await db.Conversations.FirstAsync(c => c.Id == conversationId);
        conversation.LastMessageAt = message.CreatedAt;

        await db.SaveChangesAsync();
        return message;
    }

    // Called after every Messages.Create in the orchestrator's tool loop — running totals
    // let cost-per-conversation/cost-per-order be queried directly instead of re-deriving
    // it from provider logs.
    public async Task RecordUsageAsync(Guid conversationId, long inputTokens, long outputTokens, long cacheReadTokens, long cacheCreationTokens)
    {
        var conversation = await db.Conversations.FirstAsync(c => c.Id == conversationId);
        conversation.TotalInputTokens += inputTokens;
        conversation.TotalOutputTokens += outputTokens;
        conversation.TotalCacheReadInputTokens += cacheReadTokens;
        conversation.TotalCacheCreationInputTokens += cacheCreationTokens;
        await db.SaveChangesAsync();
    }

    // 12 messages (~6 exchanges) comfortably covers a single-intent shopping conversation
    // (browse -> size/fit -> contact details -> payment -> confirm) without dragging in
    // stale context from far earlier. Kept smaller than before deliberately: once a
    // conversation outgrows this window it starts sliding, and every slide forces a fresh
    // (uncached) resend of whatever's still in it — the tools/system prompt no longer ride
    // along with that resend (see CreateMessageAsync's cache breakpoints), so the smaller
    // the window, the cheaper each slide is.
    public async Task<List<ConversationMessageResponse>> GetHistoryAsync(Guid conversationId, int limit = 12)
    {
        var messages = await db.ConversationMessages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        messages.Reverse();
        return messages.Select(ToDto).ToList();
    }

    public async Task<List<ConversationSummaryResponse>> ListForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        return await db.Conversations
            .Where(c => c.StoreId == store.Id)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ConversationSummaryResponse(c.Id, c.CustomerDisplayName, c.ExternalUserId, c.LastMessageAt, c.CreatedAt))
            .ToListAsync();
    }

    public async Task<ConversationDetailResponse> GetDetailForOwnStoreAsync(string clerkUserId, Guid conversationId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var conversation = await db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.StoreId == store.Id)
            ?? throw new NotFoundException("Conversation not found.");

        var messages = conversation.Messages.OrderBy(m => m.CreatedAt).Select(ToDto).ToList();

        return new ConversationDetailResponse(conversation.Id, conversation.CustomerDisplayName, conversation.ExternalUserId, conversation.LastMessageAt, conversation.CreatedAt, messages);
    }

    private static ConversationMessageResponse ToDto(ConversationMessage m) =>
        new(m.Id, m.Direction.ToString(), m.Content, m.CreatedAt);
}
