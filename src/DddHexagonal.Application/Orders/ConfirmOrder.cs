using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Orders;

public sealed record ConfirmOrderCommand(Guid Id);

public sealed class ConfirmOrderUseCase(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IUseCase<ConfirmOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> ExecuteAsync(ConfirmOrderCommand request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Order", request.Id);

        order.Confirm();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }
}
