using DddHexagonal.Application.Common;
using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace DddHexagonal.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<PagedResult<Order>> ListAsync(PageQuery page, CancellationToken cancellationToken = default) =>
        db.Orders.AsNoTracking().Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ThenBy(o => o.Id)
            .ToPagedAsync(page, cancellationToken);

    public Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        db.Orders.AnyAsync(o => o.CustomerId == customerId, cancellationToken);

    public Task<bool> ExistsForProductAsync(Guid productId, CancellationToken cancellationToken = default) =>
        db.Orders.AnyAsync(o => o.Items.Any(i => i.ProductId == productId), cancellationToken);

    public void Add(Order order) => db.Orders.Add(order);

    public void Remove(Order order) => db.Orders.Remove(order);
}
