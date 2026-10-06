using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Tests.Products;

public sealed class ProductTests
{
    private static Product NewProduct(int stock = 5) =>
        Product.Create("Keyboard", Sku.Create("KEY-001"), Money.Create(99.9m), stock);

    [Fact]
    public void Create_SetsData()
    {
        var product = NewProduct();
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Keyboard", product.Name);
        Assert.Equal("KEY-001", product.Sku.Value);
        Assert.Equal(99.9m, product.Price.Amount);
        Assert.Equal(5, product.Stock);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Create_RejectsBlankName(string? name) =>
        Assert.Throws<DomainException>(() => Product.Create(name!, Sku.Create("ABC"), Money.Zero, 0));

    [Fact]
    public void Create_RejectsTooLongName() =>
        Assert.Throws<DomainException>(() => Product.Create(new string('a', 201), Sku.Create("ABC"), Money.Zero, 0));

    [Fact]
    public void Create_RejectsNegativeStock() =>
        Assert.Throws<DomainException>(() => Product.Create("X", Sku.Create("ABC"), Money.Zero, -1));

    [Fact]
    public void Update_ChangesNameSkuAndPrice()
    {
        var product = NewProduct();
        product.Update("Mouse", Sku.Create("MOU-1"), Money.Create(10m));
        Assert.Equal("Mouse", product.Name);
        Assert.Equal("MOU-1", product.Sku.Value);
        Assert.Equal(10m, product.Price.Amount);
    }

    [Fact]
    public void Update_RejectsBlankName() =>
        Assert.Throws<DomainException>(() => NewProduct().Update("", Sku.Create("ABC"), Money.Zero));

    [Theory]
    [InlineData(3, 8)]
    [InlineData(-5, 0)]
    public void AdjustStock_AppliesDelta(int delta, int expected)
    {
        var product = NewProduct();
        product.AdjustStock(delta);
        Assert.Equal(expected, product.Stock);
    }

    [Fact]
    public void AdjustStock_CannotMakeStockNegative()
    {
        var product = NewProduct();
        Assert.Throws<DomainException>(() => product.AdjustStock(-6));
        Assert.Equal(5, product.Stock);
    }

    [Fact]
    public void AdjustStock_RejectsZeroDelta() => Assert.Throws<DomainException>(() => NewProduct().AdjustStock(0));
}
