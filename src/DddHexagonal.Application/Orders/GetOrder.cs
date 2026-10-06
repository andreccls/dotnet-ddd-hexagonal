using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Orders;

public sealed record GetOrderQuery(Guid Id);

public sealed class GetOrderUseCase(IOrderRepository orders) : IUseCase<GetOrderQuery, OrderResponse>
{
    public async Task<OrderResponse> ExecuteAsync(GetOrderQuery request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Order", request.Id);
        return OrderResponse.From(order);
    }
}
