namespace Mooncake.EcommercePlatform.Verification.Tests;

using Xunit;
using Mooncake.EcommercePlatform.Application.Common.DTOs;
using Mooncake.EcommercePlatform.Application.Common.Exceptions;
using Mooncake.EcommercePlatform.Application.Common.Helpers;

public sealed class PaginationContractTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1001, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Validate_RejectsOutOfRangePages(int page, int pageSize)
    {
        var error = Assert.Throws<HttpException>(() =>
            PagedQueryHelper.Validate(new PagedQuery { Page = page, PageSize = pageSize }));
        Assert.Equal(400, error.StatusCode);
        Assert.Equal("VALIDATION_ERROR", error.ErrorCode);
    }

    [Fact]
    public void Validate_RejectsReversedDateRangeAndUnknownSort()
    {
        Assert.Throws<HttpException>(() => PagedQueryHelper.Validate(new PagedQuery
        {
            From = new DateTime(2026, 11, 6, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc)
        }));
        Assert.Throws<HttpException>(() => PagedQueryHelper.ValidateSort("amount; DROP TABLE orders", "createdAtDesc"));
    }

    [Fact]
    public void ApplyPaging_PreservesBoundariesAndExactMetadata()
    {
        var query = new PagedQuery { Page = 3, PageSize = 10 };
        var rows = PagedQueryHelper.ApplyPaging(Enumerable.Range(1, 25).AsQueryable(), query).ToArray();
        var page = new PagedResult<int>(rows, query.Page, query.PageSize, 25);
        Assert.Equal([21, 22, 23, 24, 25], page.Items);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }
}
