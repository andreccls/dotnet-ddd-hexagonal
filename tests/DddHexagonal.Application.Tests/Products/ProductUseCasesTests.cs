using DddHexagonal.Application.Common;
using DddHexagonal.Application.Products;
using DddHexagonal.Application.Tests.Fakes;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Tests.Products;

public sealed class ProductUseCasesTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _uow = new();

    private async Task<ProductResponse> Seed(string sku = "KEY-001", int stock = 5) =>
        await new CreateProductUseCase(_products, _uow).ExecuteAsync(new CreateProductCommand("Keyboard", sku, 99.9m, stock));

    [Fact]
    public async Task Create_PersistsAndReturnsResponse()
    {
        var response = await Seed("key-001");
        Assert.Equal("KEY-001", response.Sku);
        Assert.Equal(99.9m, response.Price);
        Assert.Equal(5, response.Stock);
        Assert.Single(_products.Items);
        Assert.Equal(1, _uow.SaveCount);
    }

    [Fact]
    public async Task Create_WithDuplicatedSku_ThrowsConflict()
    {
        await Seed();
        await Assert.ThrowsAsync<ConflictException>(() => Seed());
    }

    [Fact]
    public async Task Create_WithNegativePrice_Throws() =>
        await Assert.ThrowsAsync<DomainException>(() =>
            new CreateProductUseCase(_products, _uow).ExecuteAsync(new CreateProductCommand("X", "ABC", -1m, 0)));

    [Fact]
    public async Task Get_ReturnsProduct_OrThrowsNotFound()
    {
        var created = await Seed();
        var useCase = new GetProductUseCase(_products);
        Assert.Equal(created, await useCase.ExecuteAsync(new GetProductQuery(created.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new GetProductQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task List_IsPaginated()
    {
        await Seed("AAA");
        await Seed("BBB");
        var page = await new ListProductsUseCase(_products).ExecuteAsync(new PageQuery(1, 1));
        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task Update_ChangesData_AndAllowsKeepingOwnSku()
    {
        var created = await Seed();
        var response = await new UpdateProductUseCase(_products, _uow).ExecuteAsync(
            new UpdateProductCommand("Mouse", "KEY-001", 10m) { Id = created.Id });
        Assert.Equal("Mouse", response.Name);
        Assert.Equal(10m, response.Price);
    }

    [Fact]
    public async Task Update_WithSkuOfAnotherProduct_ThrowsConflict()
    {
        await Seed("AAA");
        var other = await Seed("BBB");
        await Assert.ThrowsAsync<ConflictException>(() => new UpdateProductUseCase(_products, _uow).ExecuteAsync(
            new UpdateProductCommand("X", "AAA", 1m) { Id = other.Id }));
    }

    [Fact]
    public async Task Update_Unknown_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() => new UpdateProductUseCase(_products, _uow).ExecuteAsync(
            new UpdateProductCommand("X", "AAA", 1m) { Id = Guid.NewGuid() }));

    [Fact]
    public async Task AdjustStock_AddsRemovesAndRefusesNegative()
    {
        var created = await Seed(stock: 5);
        var useCase = new AdjustProductStockUseCase(_products, _uow);

        Assert.Equal(8, (await useCase.ExecuteAsync(new AdjustProductStockCommand(3) { Id = created.Id })).Stock);
        Assert.Equal(0, (await useCase.ExecuteAsync(new AdjustProductStockCommand(-8) { Id = created.Id })).Stock);
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new AdjustProductStockCommand(-1) { Id = created.Id }));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new AdjustProductStockCommand(1) { Id = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Delete_RemovesProduct()
    {
        var created = await Seed();
        await new DeleteProductUseCase(_products, _orders, _uow).ExecuteAsync(new DeleteProductCommand(created.Id));
        Assert.Empty(_products.Items);
    }

    [Fact]
    public async Task Delete_Unknown_ThrowsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteProductUseCase(_products, _orders, _uow).ExecuteAsync(new DeleteProductCommand(Guid.NewGuid())));

    [Fact]
    public async Task Delete_ReferencedByOrder_ThrowsConflict()
    {
        var created = await Seed();
        _orders.Add(Order.Create(Guid.NewGuid(), [new OrderLine(created.Id, Money.Zero, 1)], DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<ConflictException>(() =>
            new DeleteProductUseCase(_products, _orders, _uow).ExecuteAsync(new DeleteProductCommand(created.Id)));
        Assert.Single(_products.Items);
    }
}
