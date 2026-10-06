using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;

namespace DddHexagonal.Application.Orders;

public sealed class ListOrdersUseCase(IOrderRepository orders)
    : IUseCase<PageQuery, PagedResult<OrderResponse>>
{
    public async Task<PagedResult<OrderResponse>> ExecuteAsync(PageQuery request, CancellationToken cancellationToken = default) =>
        (await orders.ListAsync(request, cancellationToken)).Map(OrderResponse.From);
}
