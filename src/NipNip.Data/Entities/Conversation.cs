namespace NipNip.Data.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string ExternalUserId { get; set; } = "";
    public string? CustomerDisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastMessageAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<ConversationMessage> Messages { get; set; } = [];
}
