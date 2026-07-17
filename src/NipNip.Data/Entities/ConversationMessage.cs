using NipNip.Data.Enums;

namespace NipNip.Data.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public ConversationMessageDirection Direction { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? ExternalMessageId { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
