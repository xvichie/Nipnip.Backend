namespace NipNip.Data.Entities;

public class Cart
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string SessionId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public Store Store { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = [];
    public ICollection<CartBundleItem> BundleItems { get; set; } = [];
}
