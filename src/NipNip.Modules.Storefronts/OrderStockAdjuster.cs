using NipNip.Data.Entities;

namespace NipNip.Modules.Storefronts;

/// <summary>
/// Applies/reverses the stock effect of an order's line items when its status moves into or out
/// of Cancelled. Stock is decremented up front at checkout (that decrement is the "reservation"),
/// so Confirmed/Shipped/Delivered need no further stock change — only Cancelled (and un-cancelling)
/// touches it. Requires order.Items (and each item's Variant) to already be loaded.
///
/// Bundle items resolve to a product's variants collectively with no single variant to credit
/// back precisely, so they're intentionally left untouched here (unlike ordinary items, which
/// each pin one exact ProductVariant).
/// </summary>
public static class OrderStockAdjuster
{
    public static void Release(Order order)
    {
        foreach (var item in order.Items)
        {
            if (item.Variant.Stock is null) continue;
            item.Variant.Stock += item.Quantity;
        }
    }

    public static void Reserve(Order order)
    {
        foreach (var item in order.Items)
        {
            if (item.Variant.Stock is not null && item.Variant.Stock.Value < item.Quantity)
                throw new ArgumentException($"Not enough stock for '{item.Variant.Sku}' to reinstate this order.");
        }

        foreach (var item in order.Items)
        {
            if (item.Variant.Stock is null) continue;
            item.Variant.Stock -= item.Quantity;
        }
    }
}
