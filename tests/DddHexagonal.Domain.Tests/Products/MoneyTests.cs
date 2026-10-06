using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Tests.Products;

public sealed class MoneyTests
{
    [Fact]
    public void Create_AcceptsZeroAndPositive()
    {
        Assert.Equal(0m, Money.Zero.Amount);
        Assert.Equal(10.5m, Money.Create(10.5m).Amount);
    }

    [Fact]
    public void Create_RoundsToTwoDecimals() => Assert.Equal(1.01m, Money.Create(1.005m).Amount);

    [Fact]
    public void Create_RejectsNegative() => Assert.Throws<DomainException>(() => Money.Create(-0.01m));

    [Fact]
    public void Multiply_And_Add_ComputeNewValues()
    {
        Assert.Equal(30m, Money.Create(10m).Multiply(3).Amount);
        Assert.Equal(15m, Money.Create(10m).Add(Money.Create(5m)).Amount);
    }

    [Fact]
    public void Multiply_RejectsNegativeFactor() => Assert.Throws<DomainException>(() => Money.Create(1m).Multiply(-1));

    [Fact]
    public void Equality_IsByAmount() => Assert.Equal(Money.Create(1m), Money.Create(1.00m));

    [Fact]
    public void ToString_UsesInvariantCulture() => Assert.Equal("10.50", Money.Create(10.5m).ToString());
}
