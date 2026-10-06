using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Orders;

namespace DddHexagonal.Application.Orders;

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<OrderItemRequest> Items);

public sealed class CreateOrderUseCase(
    IOrderRepository orders,
    ICustomerRepository customers,
    IProductRepository products,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IUseCase<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> ExecuteAsync(CreateOrderCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw NotFoundException.For("Customer", request.CustomerId);
        customer.EnsureActive();

        var items = request.Items ?? [];
        var stock = await OrderStock.LoadProductsAsync(products, items.Select(i => i.ProductId), cancellationToken);
        var lines = OrderStock.BuildLines(items, stock);

        var order = Order.Create(customer.Id, lines, clock.GetUtcNow());
        OrderStock.Reserve(lines, stock);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }
}
