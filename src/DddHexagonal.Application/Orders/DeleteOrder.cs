using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Orders;

public sealed record DeleteOrderCommand(Guid Id);

public sealed class DeleteOrderUseCase(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IUseCase<DeleteOrderCommand, Unit>
{
    public async Task<Unit> ExecuteAsync(DeleteOrderCommand request, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Order", request.Id);

        order.EnsureCanBeRemoved();
        orders.Remove(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
