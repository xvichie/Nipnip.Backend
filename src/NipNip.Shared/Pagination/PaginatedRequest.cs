namespace NipNip.Shared.Pagination;

public record PaginatedRequest(int Page = 1, int PageSize = 20);
