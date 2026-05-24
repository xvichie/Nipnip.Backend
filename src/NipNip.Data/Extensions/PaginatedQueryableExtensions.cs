using Microsoft.EntityFrameworkCore;
using NipNip.Shared.Pagination;

namespace NipNip.Data.Extensions;

public static class PaginatedQueryableExtensions
{
    public static async Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<T>(items, totalCount, page, pageSize, totalPages);
    }

    public static Task<PaginatedResult<T>> ToPaginatedResultAsync<T>(
        this IQueryable<T> query,
        PaginatedRequest request,
        CancellationToken cancellationToken = default) =>
        query.ToPaginatedResultAsync(request.Page, request.PageSize, cancellationToken);
}
