namespace NipNip.Data.Entities;

public class ContactMessage
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
}
