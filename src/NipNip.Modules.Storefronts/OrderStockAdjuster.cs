using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;

namespace NipNip.Modules.Storefronts;

/// <summary>
/// Applies/reverses the stock effect of an order's line items when its status moves into or out
/// of Cancelled. Stock is decremented up front at checkout (that decrement is the "reservation"),
/// so Confirmed/Shipped/Delivered need no further stock change — only Cancelled (and un-cancelling)
/// touches it. Requires order.Items (with Variant) and order.BundleItems (with StockAllocations)
/// to already be loaded.
///
/// Every adjustment is an atomic conditional UPDATE against the database (ExecuteUpdateAsync),
/// never a read-then-write on the tracked entity — so concurrent cancellations/orders touching the
/// same variant can't silently clobber one another. Callers must wrap the call together with their
/// own order.Status SaveChangesAsync in one DB transaction: these methods run outside the change
/// tracker, so without an enclosing transaction a failure partway through Reserve (e.g. the 2nd of
/// 3 bundle allocations is out of stock) would leave the 1st allocation's decrement applied with
/// no rollback.
///
/// Bundle items resolve to one or more of a product's variants via the exact
/// OrderBundleItemVariantAllocation breakdown recorded at checkout (CartService.
/// DecrementBundleStockAsync) — replaying that recorded split rather than re-deriving one, since
/// current stock levels may no longer match what checkout saw.
/// </summary>
public static class OrderStockAdjuster
{
    public static async Task ReleaseAsync(AppDbContext db, Order order)
    {
        foreach (var item in order.Items)
        {
            if (item.Variant.Stock is null) continue;
            await db.ProductVariants
                .Where(v => v.Id == item.VariantId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock + item.Quantity));
        }

        foreach (var allocation in order.BundleItems.SelectMany(i => i.StockAllocations))
        {
            await db.ProductVariants
                .Where(v => v.Id == allocation.VariantId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock + allocation.Quantity));
        }
    }

    public static async Task ReserveAsync(AppDbContext db, Order order)
    {
        foreach (var item in order.Items)
        {
            if (item.Variant.Stock is null) continue;

            var rows = await db.ProductVariants
                .Where(v => v.Id == item.VariantId && v.Stock >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - item.Quantity));

            if (rows == 0)
                throw new ArgumentException($"Not enough stock for '{item.Variant.Sku}' to reinstate this order.");
        }

        foreach (var allocation in order.BundleItems.SelectMany(i => i.StockAllocations))
        {
            var rows = await db.ProductVariants
                .Where(v => v.Id == allocation.VariantId && v.Stock >= allocation.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Stock, v => v.Stock - allocation.Quantity));

            if (rows == 0)
                throw new ArgumentException("Not enough stock to reinstate this order's bundle item.");
        }
    }
}
