using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Customers;

public sealed record CreateCustomerCommand(string Name, string Email, string Document, string? Phone);

public sealed class CreateCustomerUseCase(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IUseCase<CreateCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> ExecuteAsync(CreateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var email = Email.Create(request.Email);
        if (await customers.EmailExistsAsync(email, null, cancellationToken))
        {
            throw new ConflictException($"Email '{email}' is already in use.");
        }

        var customer = Customer.Create(request.Name, email, Cpf.Create(request.Document), CustomerInput.ParsePhone(request.Phone));
        customers.Add(customer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerResponse.From(customer);
    }
}
