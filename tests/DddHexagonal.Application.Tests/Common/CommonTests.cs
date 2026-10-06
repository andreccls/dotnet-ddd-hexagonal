using DddHexagonal.Application.Common;

namespace DddHexagonal.Application.Tests.Common;

public sealed class CommonTests
{
    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-5, 500, 1, 100)]
    [InlineData(3, 10, 3, 10)]
    public void PageQuery_ClampsValues(int page, int size, int expectedPage, int expectedSize)
    {
        var query = new PageQuery(page, size);
        Assert.Equal(expectedPage, query.Page);
        Assert.Equal(expectedSize, query.PageSize);
        Assert.Equal((expectedPage - 1) * expectedSize, query.Skip);
    }

    [Fact]
    public void PageQuery_HasDefaults()
    {
        var query = new PageQuery();
        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
    }

    [Fact]
    public void PagedResult_ComputesTotalPagesAndMaps()
    {
        var result = new PagedResult<int>([1, 2], 1, 2, 5);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal([2, 4], result.Map(i => i * 2).Items);
        Assert.Equal(5, result.Map(i => i * 2).TotalCount);
    }

    [Fact]
    public void Unit_HasSingleValue() => Assert.Equal(default, Unit.Value);
}
