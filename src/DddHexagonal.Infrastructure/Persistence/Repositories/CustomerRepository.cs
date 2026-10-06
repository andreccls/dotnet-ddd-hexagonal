using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace DddHexagonal.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<PagedResult<Customer>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        db.Customers.AsNoTracking().OrderBy(c => c.Name).ThenBy(c => c.Id).ToPagedAsync(page, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, Guid? exceptCustomerId, CancellationToken cancellationToken = default) =>
        db.Customers.AnyAsync(c => c.Email == email && c.Id != exceptCustomerId, cancellationToken);

    public void Add(Customer customer) => db.Customers.Add(customer);

    public void Remove(Customer customer) => db.Customers.Remove(customer);
}
