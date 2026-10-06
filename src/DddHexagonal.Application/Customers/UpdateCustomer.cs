using System.Text.Json.Serialization;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Customers;

public sealed record UpdateCustomerCommand(string Name, string Email, string Document, string? Phone)
{
    /// <summary>Comes from the route, never from the body.</summary>
    [JsonIgnore]
    public Guid Id { get; init; }
}

public sealed class UpdateCustomerUseCase(ICustomerRepository customers, IUnitOfWork unitOfWork)
    : IUseCase<UpdateCustomerCommand, CustomerResponse>
{
    public async Task<CustomerResponse> ExecuteAsync(UpdateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Customer", request.Id);

        var email = Email.Create(request.Email);
        if (await customers.EmailExistsAsync(email, customer.Id, cancellationToken))
        {
            throw new ConflictException($"Email '{email}' is already in use.");
        }

        customer.Update(request.Name, email, Cpf.Create(request.Document), CustomerInput.ParsePhone(request.Phone));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CustomerResponse.From(customer);
    }
}
