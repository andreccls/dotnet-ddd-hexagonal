using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Customers;

public sealed record DeleteCustomerCommand(Guid Id);

public sealed class DeleteCustomerUseCase(ICustomerRepository customers, IOrderRepository orders, IUnitOfWork unitOfWork)
    : IUseCase<DeleteCustomerCommand, Unit>
{
    public async Task<Unit> ExecuteAsync(DeleteCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Customer", request.Id);

        // Orders reference customers by id only, so the database cannot protect us: the rule lives here.
        if (await orders.ExistsForCustomerAsync(customer.Id, cancellationToken))
        {
            throw new ConflictException("Customer has orders and cannot be deleted. Deactivate it instead.");
        }

        customers.Remove(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
