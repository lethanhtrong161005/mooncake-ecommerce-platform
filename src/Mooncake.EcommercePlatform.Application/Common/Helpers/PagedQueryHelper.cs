namespace Mooncake.EcommercePlatform.Application.Common.Helpers;

using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;

/// <summary>Validates bounded list queries and applies offset paging after a stable sort.</summary>
public static class PagedQueryHelper
{
    private const string InvalidQueryMessage = "Invalid pagination or filter parameters.";
    private const string InvalidSortMessage = "Unsupported sort value.";
    private const string ValidationErrorCode = "VALIDATION_ERROR";

    public static void Validate(PagedQuery query, int maxPageSize = 100)
    {
        if (query.Page is < 1 or > 1000 || query.PageSize < 1 || query.PageSize > maxPageSize
            || query.Q?.Length > 100 || query.From?.Kind == DateTimeKind.Local
            || query.To?.Kind == DateTimeKind.Local || query.From > query.To)
            throw new HttpException(400, InvalidQueryMessage, ValidationErrorCode);
    }

    public static void ValidateSort(string? sort, params string[] allowed)
    {
        if (sort is not null && !allowed.Contains(sort, StringComparer.Ordinal))
            throw new HttpException(400, InvalidSortMessage, ValidationErrorCode);
    }

    public static IQueryable<T> ApplyPaging<T>(IQueryable<T> sortedQuery, PagedQuery query) =>
        sortedQuery.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
}
