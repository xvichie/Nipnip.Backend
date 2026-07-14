namespace NipNip.Data.Entities;

public class OrderNote
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public Order Order { get; set; } = null!;
}
