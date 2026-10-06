using System.Text.Json.Serialization;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Orders;

public sealed record UpdateOrderItemsCommand(IReadOnlyList<OrderItemRequest> Items)
{
    [JsonIgnore]
    public Guid Id { get; init; }
}

public sealed class UpdateOrderItemsUseCase(IOrderRepository orders, IProductRepository products, IUnitOfWork unitOfWork)
    : IUseCase<UpdateOrderItemsCommand, OrderResponse>
{
    public async Task<OrderResponse> ExecuteAsync(UpdateOrderItemsCommand request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Order", request.Id);

        var items = request.Items ?? [];
        var previous = order.Items.ToList();
        var stock = await OrderStock.LoadProductsAsync(
            products, previous.Select(i => i.ProductId).Concat(items.Select(i => i.ProductId)), cancellationToken);
        var lines = OrderStock.BuildLines(items, stock);

        order.ReplaceItems(lines);               // validates status + items BEFORE any stock moves
        OrderStock.Release(previous, stock);
        OrderStock.Reserve(lines, stock);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }
}
