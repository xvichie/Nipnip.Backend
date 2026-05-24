namespace NipNip.Shared.Pagination;

public record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
)
{
    public PaginatedResult<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        new(Items.Select(mapper).ToList(), TotalCount, Page, PageSize, TotalPages);
}
