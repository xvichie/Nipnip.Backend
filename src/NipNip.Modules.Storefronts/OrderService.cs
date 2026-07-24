using Microsoft.EntityFrameworkCore;
using NipNip.Data;
using NipNip.Data.Entities;
using NipNip.Data.Enums;
using NipNip.Data.Extensions;
using NipNip.Modules.Storefronts.DTOs;
using NipNip.Modules.Storefronts.Extensions;
using NipNip.Shared.Exceptions;
using NipNip.Shared.Pagination;

namespace NipNip.Modules.Storefronts;

public class OrderService(AppDbContext db, StoreService storeService)
{
    public async Task<PaginatedResult<OrderDetailResponse>> GetAllForOwnStoreAsync(
        string clerkUserId,
        PaginatedRequest pagination,
        string? status = null,
        int? month = null,
        int? year = null)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var query = OrdersWithIncludes().Where(o => o.StoreId == store.Id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                throw new ArgumentException("Status must be one of: Pending, Confirmed, Shipped, Delivered, Cancelled.");

            query = query.Where(o => o.Status == parsedStatus);
        }

        if (year.HasValue && month.HasValue)
        {
            var start = new DateTimeOffset(year.Value, month.Value, 1, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddMonths(1);
            query = query.Where(o => o.CreatedAt >= start && o.CreatedAt < end);
        }
        else if (year.HasValue)
        {
            var start = new DateTimeOffset(year.Value, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(year.Value + 1, 1, 1, 0, 0, 0, TimeSpan.Zero);
            query = query.Where(o => o.CreatedAt >= start && o.CreatedAt < end);
        }

        var result = await query.OrderByDescending(o => o.CreatedAt).ToPaginatedResultAsync(pagination);
        return result.Map(o => o.ToDetailDto());
    }

    public async Task<OrderDetailResponse> UpdateStatusAsync(string clerkUserId, Guid id, UpdateOrderStatusRequest request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            throw new ArgumentException("Status must be one of: Pending, Confirmed, Shipped, Delivered, Cancelled.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var order = await GetOwnOrderAsync(store.Id, id);

        order.Status = status;
        await db.SaveChangesAsync();

        return order.ToDetailDto();
    }

    public async Task<OrderDetailResponse> UpdatePaymentConfirmedAsync(string clerkUserId, Guid id, UpdatePaymentConfirmedRequest request)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var order = await GetOwnOrderAsync(store.Id, id);

        order.PaymentConfirmedAt = request.Confirmed ? (order.PaymentConfirmedAt ?? DateTimeOffset.UtcNow) : null;
        await db.SaveChangesAsync();

        return order.ToDetailDto();
    }

    public async Task<OrderDetailResponse> AddNoteAsync(string clerkUserId, Guid id, CreateOrderNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ArgumentException("Note content is required.");

        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        var order = await GetOwnOrderAsync(store.Id, id);

        db.OrderNotes.Add(new OrderNote
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            Content = request.Content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        return order.ToDetailDto();
    }

    public async Task<List<MonthlyOrderSummary>> GetMonthlySummaryForOwnStoreAsync(
        string clerkUserId,
        string? status = null)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);

        var query = db.Orders.Where(o => o.StoreId == store.Id);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                throw new ArgumentException("Status must be one of: Pending, Confirmed, Shipped, Delivered, Cancelled.");

            query = query.Where(o => o.Status == parsedStatus);
        }
        else
        {
            // Earnings default to excluding cancelled orders; pass status=Cancelled to inspect those explicitly.
            query = query.Where(o => o.Status != OrderStatus.Cancelled);
        }

        var rows = await query
            .Select(o => new { o.CreatedAt, o.Total, ItemCount = o.Items.Sum(i => i.Quantity) })
            .ToListAsync();

        return rows
            .GroupBy(o => new { o.CreatedAt.UtcDateTime.Year, o.CreatedAt.UtcDateTime.Month })
            .Select(g =>
            {
                var revenue = g.Sum(o => o.Total);
                var orderCount = g.Count();
                var productsSold = g.Sum(o => o.ItemCount);
                return new MonthlyOrderSummary(
                    g.Key.Year,
                    g.Key.Month,
                    Math.Round(revenue, 2),
                    orderCount,
                    productsSold,
                    orderCount > 0 ? Math.Round(revenue / orderCount, 2) : 0,
                    productsSold > 0 ? Math.Round(revenue / productsSold, 2) : 0
                );
            })
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
            .ToList();
    }

    public async Task<int> GetNewOrderCountForOwnStoreAsync(string clerkUserId)
    {
        var store = await storeService.GetOwnStoreAsync(clerkUserId);
        return await db.Orders.CountAsync(o => o.StoreId == store.Id && o.Status == OrderStatus.Pending);
    }

    private async Task<Order> GetOwnOrderAsync(Guid storeId, Guid id)
    {
        return await OrdersWithIncludes().FirstOrDefaultAsync(o => o.Id == id && o.StoreId == storeId)
            ?? throw new NotFoundException("Order not found.");
    }

    private IQueryable<Order> OrdersWithIncludes() =>
        db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
            .Include(o => o.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.OptionValues).ThenInclude(ov => ov.OptionValue).ThenInclude(pov => pov.ProductOption)
            .Include(o => o.BundleItems).ThenInclude(i => i.Bundle)
            .Include(o => o.Notes);
}
