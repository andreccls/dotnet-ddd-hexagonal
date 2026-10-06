using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Orders;

public sealed record CancelOrderCommand(Guid Id);

public sealed class CancelOrderUseCase(IOrderRepository orders, IProductRepository products, IUnitOfWork unitOfWork)
    : IUseCase<CancelOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> ExecuteAsync(CancelOrderCommand request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Order", request.Id);

        order.Cancel();
        var stock = await OrderStock.LoadProductsAsync(products, order.Items.Select(i => i.ProductId), cancellationToken);
        OrderStock.Release(order.Items, stock);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }
}
