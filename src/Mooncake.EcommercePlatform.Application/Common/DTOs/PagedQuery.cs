namespace Mooncake.EcommercePlatform.Application.Common.DTOs;

/// <summary>Common page and filter parameters for list endpoints.</summary>
public record PagedQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Q { get; init; }
    public string? Sort { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}

/// <summary>Stable page response with an exact filtered total.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1 && TotalPages > 0;
}
