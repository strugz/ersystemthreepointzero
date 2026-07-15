using ERSystem.Web.Application.Common;
using ERSystem.Web.Infrastructure.Persistence;

namespace ERSystem.Web.Tests.Unit;

public sealed class InMemoryPagingTests
{
    [Theory]
    [InlineData(1, 3, new[] { 1, 2, 3 })]
    [InlineData(2, 3, new[] { 4, 5, 6 })]
    [InlineData(3, 3, new[] { 7 })]
    [InlineData(4, 3, new int[0])]
    public void Paging_returns_the_requested_slice_and_preserves_the_total(
        int page, int pageSize, int[] expectedItems)
    {
        IReadOnlyList<int> source = [1, 2, 3, 4, 5, 6, 7];

        var result = source.ToInMemoryPagedResult(new PagedRequest { Page = page, PageSize = pageSize });

        Assert.Equal(expectedItems, result.Items);
        Assert.Equal(source.Count, result.Total);
        Assert.Equal(page, result.Page);
        Assert.Equal(pageSize, result.PageSize);
    }

    [Fact]
    public void Paging_preserves_deterministic_composite_order()
    {
        IReadOnlyList<TestRow> source =
        [
            new("A", 2),
            new("A", 3),
            new("B", 1)
        ];

        var result = source.ToInMemoryPagedResult(new PagedRequest { Page = 1, PageSize = 2 });

        Assert.Collection(result.Items,
            item => Assert.Equal(("A", 2), (item.PrimarySort, item.Id)),
            item => Assert.Equal(("A", 3), (item.PrimarySort, item.Id)));
    }

    private sealed record TestRow(string PrimarySort, int Id);
}
