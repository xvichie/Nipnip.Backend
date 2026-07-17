namespace NipNip.Data.Entities;

public class KnowledgeBaseSection
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";

    /// <summary>Null until embedded — rows carried over from the old fixed fields start this way.</summary>
    public float[]? Embedding { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;
}
