using DddHexagonal.Application.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Ports.Out;

/// <summary>Outbound port. Add/Remove only register intent; <see cref="IUnitOfWork"/> persists.</summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Customer>> ListAsync(PageQuery page, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, Guid? exceptCustomerId, CancellationToken cancellationToken = default);

    void Add(Customer customer);

    void Remove(Customer customer);
}
