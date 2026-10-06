using DddHexagonal.Application.Common;
using DddHexagonal.Application.Customers;
using DddHexagonal.Application.Orders;
using DddHexagonal.Application.Products;
using DddHexagonal.Application.Tests.Fakes;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Tests.Orders;

public sealed class OrderUseCasesTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 6, 7, 8, 9, TimeSpan.Zero);

    private readonly InMemoryCustomerRepository _customers = new();
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _uow = new();

    private CreateOrderUseCase Create() => new(_orders, _customers, _products, _uow, new FixedClock(Now));

    private async Task<Guid> SeedCustomer(bool active = true)
    {
        var customer = await new CreateCustomerUseCase(_customers, _uow)
            .ExecuteAsync(new CreateCustomerCommand("Maria", $"{Guid.NewGuid():N}@example.com", "529.982.247-25", null));
        if (!active)
        {
            await new SetCustomerStatusUseCase(_customers, _uow).ExecuteAsync(new SetCustomerStatusCommand(customer.Id, false));
        }

        return customer.Id;
    }

    private async Task<Guid> SeedProduct(decimal price = 10m, int stock = 10) =>
        (await new CreateProductUseCase(_products, _uow)
            .ExecuteAsync(new CreateProductCommand("Item", $"SKU-{Guid.NewGuid():N}"[..12], price, stock))).Id;

    private int StockOf(Guid productId) => _products.Items.Single(p => p.Id == productId).Stock;

    [Fact]
    public async Task Create_BuildsOrder_ReservesStock_AndSnapshotsPrice()
    {
        var customerId = await SeedCustomer();
        var p1 = await SeedProduct(10m, 10);
        var p2 = await SeedProduct(2.5m, 4);

        var order = await Create().ExecuteAsync(new CreateOrderCommand(customerId, [new(p1, 2), new(p2, 4)]));

        Assert.Equal("Pending", order.Status);
        Assert.Equal(Now, order.CreatedAt);
        Assert.Equal(30m, order.Total);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(8, StockOf(p1));
        Assert.Equal(0, StockOf(p2));
        Assert.Single(_orders.Items);
    }

    [Fact]
    public async Task Create_UnknownCustomer_ThrowsNotFound()
    {
        var p = await SeedProduct();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Create().ExecuteAsync(new CreateOrderCommand(Guid.NewGuid(), [new(p, 1)])));
    }

    [Fact]
    public async Task Create_InactiveCustomer_Throws()
    {
        var customerId = await SeedCustomer(active: false);
        var p = await SeedProduct();
        await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(new CreateOrderCommand(customerId, [new(p, 1)])));
        Assert.Equal(10, StockOf(p));
    }

    [Fact]
    public async Task Create_UnknownProduct_ThrowsNotFound()
    {
        var customerId = await SeedCustomer();
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Create().ExecuteAsync(new CreateOrderCommand(customerId, [new(Guid.NewGuid(), 1)])));
    }

    [Fact]
    public async Task Create_WithoutItemsOrNullItems_Throws()
    {
        var customerId = await SeedCustomer();
        await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(new CreateOrderCommand(customerId, [])));
        await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(new CreateOrderCommand(customerId, null!)));
    }

    [Fact]
    public async Task Create_WithZeroQuantity_Throws_AndDoesNotTouchStock()
    {
        var customerId = await SeedCustomer();
        var p = await SeedProduct();
        await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(new CreateOrderCommand(customerId, [new(p, 0)])));
        Assert.Equal(10, StockOf(p));
    }

    [Fact]
    public async Task Create_WithInsufficientStock_ThrowsAndSavesNothing()
    {
        var customerId = await SeedCustomer();
        var p = await SeedProduct(stock: 1);
        var saves = _uow.SaveCount;

        await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(new CreateOrderCommand(customerId, [new(p, 2)])));

        Assert.Empty(_orders.Items);
        Assert.Equal(saves, _uow.SaveCount);
    }

    private async Task<OrderResponse> SeedOrder(Guid customerId, params (Guid Product, int Qty)[] items) =>
        await Create().ExecuteAsync(new CreateOrderCommand(customerId, [.. items.Select(i => new OrderItemRequest(i.Product, i.Qty))]));

    [Fact]
    public async Task Get_ReturnsOrder_OrThrowsNotFound()
    {
        var order = await SeedOrder(await SeedCustomer(), (await SeedProduct(), 1));
        var useCase = new GetOrderUseCase(_orders);
        Assert.Equal(order.Id, (await useCase.ExecuteAsync(new GetOrderQuery(order.Id))).Id);
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new GetOrderQuery(Guid.NewGuid())));
    }

    [Fact]
    public async Task List_IsPaginated()
    {
        var customerId = await SeedCustomer();
        var p = await SeedProduct(stock: 10);
        await SeedOrder(customerId, (p, 1));
        await SeedOrder(customerId, (p, 1));

        var page = await new ListOrdersUseCase(_orders).ExecuteAsync(new PageQuery(1, 1));

        Assert.Single(page.Items);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task UpdateItems_ReleasesOldStock_ReservesNew_AndRecomputesTotal()
    {
        var customerId = await SeedCustomer();
        var p1 = await SeedProduct(10m, 10);
        var p2 = await SeedProduct(5m, 10);
        var order = await SeedOrder(customerId, (p1, 3));

        var updated = await new UpdateOrderItemsUseCase(_orders, _products, _uow)
            .ExecuteAsync(new UpdateOrderItemsCommand([new(p1, 1), new(p2, 2)]) { Id = order.Id });

        Assert.Equal(20m, updated.Total);
        Assert.Equal(9, StockOf(p1));
        Assert.Equal(8, StockOf(p2));
    }

    [Fact]
    public async Task UpdateItems_OnConfirmedOrder_ThrowsAndKeepsStock()
    {
        var p = await SeedProduct();
        var order = await SeedOrder(await SeedCustomer(), (p, 3));
        await new ConfirmOrderUseCase(_orders, _uow).ExecuteAsync(new ConfirmOrderCommand(order.Id));

        await Assert.ThrowsAsync<DomainException>(() => new UpdateOrderItemsUseCase(_orders, _products, _uow)
            .ExecuteAsync(new UpdateOrderItemsCommand([new(p, 1)]) { Id = order.Id }));
        Assert.Equal(7, StockOf(p));
    }

    [Fact]
    public async Task UpdateItems_UnknownOrderOrProduct_ThrowsNotFound()
    {
        var p = await SeedProduct();
        var order = await SeedOrder(await SeedCustomer(), (p, 1));
        var useCase = new UpdateOrderItemsUseCase(_orders, _products, _uow);

        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new UpdateOrderItemsCommand([new(p, 1)]) { Id = Guid.NewGuid() }));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new UpdateOrderItemsCommand([new(Guid.NewGuid(), 1)]) { Id = order.Id }));
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new UpdateOrderItemsCommand(null!) { Id = order.Id }));
    }

    [Fact]
    public async Task Confirm_MovesToConfirmed_AndRejectsRepeatOrUnknown()
    {
        var order = await SeedOrder(await SeedCustomer(), (await SeedProduct(), 1));
        var useCase = new ConfirmOrderUseCase(_orders, _uow);

        Assert.Equal("Confirmed", (await useCase.ExecuteAsync(new ConfirmOrderCommand(order.Id))).Status);
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new ConfirmOrderCommand(order.Id)));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new ConfirmOrderCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task Cancel_GivesStockBack_AndIsNotRepeatable()
    {
        var p = await SeedProduct(stock: 10);
        var order = await SeedOrder(await SeedCustomer(), (p, 4));
        var useCase = new CancelOrderUseCase(_orders, _products, _uow);

        Assert.Equal("Cancelled", (await useCase.ExecuteAsync(new CancelOrderCommand(order.Id))).Status);
        Assert.Equal(10, StockOf(p));
        await Assert.ThrowsAsync<DomainException>(() => useCase.ExecuteAsync(new CancelOrderCommand(order.Id)));
        Assert.Equal(10, StockOf(p));
        await Assert.ThrowsAsync<NotFoundException>(() => useCase.ExecuteAsync(new CancelOrderCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task Delete_OnlyAfterCancel()
    {
        var order = await SeedOrder(await SeedCustomer(), (await SeedProduct(), 1));
        var delete = new DeleteOrderUseCase(_orders, _uow);

        await Assert.ThrowsAsync<DomainException>(() => delete.ExecuteAsync(new DeleteOrderCommand(order.Id)));
        await new CancelOrderUseCase(_orders, _products, _uow).ExecuteAsync(new CancelOrderCommand(order.Id));
        await delete.ExecuteAsync(new DeleteOrderCommand(order.Id));

        Assert.Empty(_orders.Items);
        await Assert.ThrowsAsync<NotFoundException>(() => delete.ExecuteAsync(new DeleteOrderCommand(order.Id)));
    }
}
