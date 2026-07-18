namespace NipNip.Data.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string ExternalUserId { get; set; } = "";
    public string? CustomerDisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }

    // Running totals across every Messages.Create call in this conversation — lets
    // cost-per-conversation/cost-per-order be queried directly instead of re-deriving
    // it from provider logs.
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public long TotalCacheReadInputTokens { get; set; }
    public long TotalCacheCreationInputTokens { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
