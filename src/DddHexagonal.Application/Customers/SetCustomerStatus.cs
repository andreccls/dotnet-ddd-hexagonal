using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Customers;

public sealed record SetCustomerStatusCommand(Guid Id, bool Active);

public sealed class SetCustomerStatusUseCase(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IUseCase<SetCustomerStatusCommand, CustomerResponse>
{
    public async Task<CustomerResponse> ExecuteAsync(SetCustomerStatusCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Customer", request.Id);

        if (request.Active)
        {
            customer.Activate();
        }
        else
        {
            customer.Deactivate();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerResponse.From(customer);
    }
}
