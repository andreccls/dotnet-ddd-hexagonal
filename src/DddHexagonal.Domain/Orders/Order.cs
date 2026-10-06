using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Orders;

public sealed class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = [];

    private Order(Guid id, Guid customerId, DateTimeOffset createdAt)
        : base(id)
    {
        CustomerId = customerId;
        CreatedAt = createdAt;
        Status = OrderStatus.Pending;
    }

    private Order()
    {
        // EF Core
    }

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;

    /// <summary>Derived on demand: never stored, so it can never be out of sync with the items.</summary>
    public Money Total => _items.Aggregate(Money.Zero, (sum, item) => sum.Add(item.Subtotal));

    public static Order Create(Guid customerId, IEnumerable<OrderLine> lines, DateTimeOffset createdAt)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("CustomerId is required.");
        }

        var order = new Order(Guid.NewGuid(), customerId, createdAt);
        order.SetItems(lines);
        return order;
    }

    public void ReplaceItems(IEnumerable<OrderLine> lines)
    {
        EnsurePending();
        SetItems(lines);
    }

    public void Confirm()
    {
        EnsurePending();
        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
        {
            throw new DomainException("Order is already cancelled.");
        }

        Status = OrderStatus.Cancelled;
    }

    /// <summary>Only cancelled orders may be removed: pending/confirmed ones are history or reserve stock.</summary>
    public void EnsureCanBeRemoved()
    {
        if (Status != OrderStatus.Cancelled)
        {
            throw new DomainException("Only cancelled orders can be removed. Cancel the order first.");
        }
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException($"Order is {Status} and cannot be changed.");
        }
    }

    private void SetItems(IEnumerable<OrderLine> lines)
    {
        var list = lines.ToList();
        if (list.Count == 0)
        {
            throw new DomainException("Order must have at least one item.");
        }

        if (list.Any(l => l.ProductId == Guid.Empty))
        {
            throw new DomainException("ProductId is required.");
        }

        if (list.Any(l => l.Quantity <= 0))
        {
            throw new DomainException("Item quantity must be greater than zero.");
        }

        if (list.Select(l => l.ProductId).Distinct().Count() != list.Count)
        {
            throw new DomainException("A product can appear only once per order.");
        }

        _items.Clear();
        _items.AddRange(list.Select(l => new OrderItem(l)));
    }
}
