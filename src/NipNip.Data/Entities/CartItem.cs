namespace NipNip.Data.Entities;

public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }

    public Cart Cart { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
