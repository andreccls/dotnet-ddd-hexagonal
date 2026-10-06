using DddHexagonal.Application.Common;
using DddHexagonal.Domain.Orders;

namespace DddHexagonal.Application.Ports.Out;

public interface IOrderRepository
{
    /// <summary>Loads the aggregate with its items.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> ListAsync(PageQuery page, CancellationToken cancellationToken = default);

    Task<bool> ExistsForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    void Add(Order order);

    void Remove(Order order);
}
