namespace NipNip.Data.Entities;

// Records exactly which ProductVariant(s) had stock decremented for a bundle order item, and how
// much of each. A bundle item pins only a Product, never a single variant (bundles don't collect
// option selections), so when a product has more than one variant the checkout-time decrement can
// be split across several. Cancelling/un-cancelling an order replays this exact breakdown instead
// of re-deriving one, which could otherwise pick different variants than checkout did if stock
// levels have moved since.
public class OrderBundleItemVariantAllocation
{
    public Guid Id { get; set; }
    public Guid OrderBundleItemId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }

    public OrderBundleItem OrderBundleItem { get; set; } = null!;
    public ProductVariant Variant { get; set; } = null!;
}
