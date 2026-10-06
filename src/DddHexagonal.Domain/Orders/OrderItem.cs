using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Orders;

/// <summary>Child entity of <see cref="Order"/>. Only the aggregate root creates it.</summary>
public sealed class OrderItem : Entity
{
    internal OrderItem(OrderLine line)
        : base(Guid.NewGuid())
    {
        ProductId = line.ProductId;
        UnitPrice = line.UnitPrice;
        Quantity = line.Quantity;
    }

    private OrderItem()
    {
        // EF Core
        UnitPrice = null!;
    }

    public Guid ProductId { get; private set; }

    /// <summary>Price snapshot at the moment the item was added.</summary>
    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money Subtotal => UnitPrice.Multiply(Quantity);
}
