using Ekomart.Application.Common;

namespace Ekomart.Infrastructure.Services;

internal static class Paging
{
    public static int PageNumber(PagedRequest request)
    {
        return Math.Max(1, request.PageNumber);
    }

    public static int PageSize(PagedRequest request)
    {
        return Math.Clamp(request.PageSize, 1, 100);
    }

    public static PagedResult<T> Result<T>(
        IReadOnlyList<T> items,
        PagedRequest request,
        int totalCount)
    {
        return new PagedResult<T>
        {
            Items = items,
            PageNumber = PageNumber(request),
            PageSize = PageSize(request),
            TotalCount = totalCount
        };
    }
}
