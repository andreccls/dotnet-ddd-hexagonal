using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Customers;
using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Tests.Fakes;

// Hand-written in-memory adapters for the outbound ports: simpler and more readable than a mocking
// framework for state-based tests, and they double as living documentation of the port contracts.

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal static class Paging
{
    public static Task<PagedResult<T>> Page<T>(IEnumerable<T> all, PageQuery page)
    {
        var list = all.ToList();
        return Task.FromResult(new PagedResult<T>([.. list.Skip(page.Skip).Take(page.PageSize)], page.Page, page.PageSize, list.Count));
    }
}

public sealed class InMemoryCustomerRepository : ICustomerRepository
{
    public List<Customer> Items { get; } = [];

    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id));

    public Task<PagedResult<Customer>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        Paging.Page(Items, page);

    public Task<bool> EmailExistsAsync(Email email, Guid? exceptCustomerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(c => c.Email.Equals(email) && c.Id != exceptCustomerId));

    public void Add(Customer customer) => Items.Add(customer);

    public void Remove(Customer customer) => Items.Remove(customer);
}

public sealed class InMemoryProductRepository : IProductRepository
{
    public List<Product> Items { get; } = [];

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>([.. Items.Where(p => ids.Contains(p.Id))]);

    public Task<PagedResult<Product>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        Paging.Page(Items, page);

    public Task<bool> SkuExistsAsync(Sku sku, Guid? exceptProductId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(p => p.Sku.Equals(sku) && p.Id != exceptProductId));

    public void Add(Product product) => Items.Add(product);

    public void Remove(Product product) => Items.Remove(product);
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    public List<Order> Items { get; } = [];

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

    public Task<PagedResult<Order>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        Paging.Page(Items, page);

    public Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(o => o.CustomerId == customerId));

    public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(o => o.Items.Any(i => i.ProductId == productId)));

    public void Add(Order order) => Items.Add(order);

    public void Remove(Order order) => Items.Remove(order);
}
