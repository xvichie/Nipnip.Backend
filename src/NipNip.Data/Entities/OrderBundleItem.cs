namespace NipNip.Data.Entities;

public class OrderBundleItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid BundleId { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtPurchase { get; set; }

    public Order Order { get; set; } = null!;
    public ProductBundle Bundle { get; set; } = null!;
}
