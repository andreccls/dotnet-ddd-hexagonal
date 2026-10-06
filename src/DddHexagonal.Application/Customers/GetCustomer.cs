using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Application.Customers;

public sealed record GetCustomerQuery(Guid Id);

public sealed class GetCustomerUseCase(ICustomerRepository customers) : IUseCase<GetCustomerQuery, CustomerResponse>
{
    public async Task<CustomerResponse> ExecuteAsync(GetCustomerQuery request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Customer", request.Id);
        return CustomerResponse.From(customer);
    }
}
