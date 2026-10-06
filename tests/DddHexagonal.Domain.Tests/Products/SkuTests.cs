using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Tests.Products;

public sealed class SkuTests
{
    [Fact]
    public void Create_UppercasesAndTrims() => Assert.Equal("ABC-123", Sku.Create("  abc-123 ").Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("bad_char")]
    public void Create_RejectsInvalid(string? raw) => Assert.Throws<DomainException>(() => Sku.Create(raw));

    [Fact]
    public void Create_RejectsTooLong() => Assert.Throws<DomainException>(() => Sku.Create(new string('A', 33)));

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(Sku.Create("abc"), Sku.Create("ABC"));
        Assert.Equal("ABC", Sku.Create("abc").ToString());
    }
}
