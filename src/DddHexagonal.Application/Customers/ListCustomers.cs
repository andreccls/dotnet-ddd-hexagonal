using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;

namespace DddHexagonal.Application.Customers;

public sealed class ListCustomersUseCase(ICustomerRepository customers)
    : IUseCase<PageQuery, PagedResult<CustomerResponse>>
{
    public async Task<PagedResult<CustomerResponse>> ExecuteAsync(PageQuery request, CancellationToken cancellationToken = default) =>
        (await customers.ListAsync(request, cancellationToken)).Map(CustomerResponse.From);
}
