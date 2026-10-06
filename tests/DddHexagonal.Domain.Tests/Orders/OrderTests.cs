using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Tests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.NewGuid();

    private static OrderLine Line(decimal price = 10m, int qty = 2, Guid? productId = null) =>
        new(productId ?? Guid.NewGuid(), Money.Create(price), qty);

    private static Order NewOrder(params OrderLine[] lines) =>
        Order.Create(CustomerId, lines.Length == 0 ? [Line()] : lines, Now);

    [Fact]
    public void Create_StartsPendingWithTotal()
    {
        var order = NewOrder(Line(10m, 2), Line(5.5m, 3));

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(CustomerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(Now, order.CreatedAt);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(36.5m, order.Total.Amount);
    }

    [Fact]
    public void Create_RejectsEmptyCustomerId() =>
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, [Line()], Now));

    [Fact]
    public void Create_RequiresAtLeastOneItem() =>
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, [], Now));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveQuantity(int qty) =>
        Assert.Throws<DomainException>(() => NewOrder(Line(qty: qty)));

    [Fact]
    public void Create_RejectsEmptyProductId() =>
        Assert.Throws<DomainException>(() => NewOrder(Line(productId: Guid.Empty)));

    [Fact]
    public void Create_RejectsDuplicatedProduct()
    {
        var productId = Guid.NewGuid();
        Assert.Throws<DomainException>(() => NewOrder(Line(productId: productId), Line(productId: productId)));
    }

    [Fact]
    public void ReplaceItems_WhilePending_RecomputesTotal()
    {
        var order = NewOrder(Line(10m, 1));
        order.ReplaceItems([Line(3m, 3)]);

        Assert.Single(order.Items);
        Assert.Equal(9m, order.Total.Amount);
    }

    [Fact]
    public void ReplaceItems_ValidatesNewItems()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(() => order.ReplaceItems([]));
    }

    [Fact]
    public void Confirm_MovesPendingToConfirmed()
    {
        var order = NewOrder();
        order.Confirm();
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_Twice_Throws()
    {
        var order = NewOrder();
        order.Confirm();
        Assert.Throws<DomainException>(order.Confirm);
    }

    [Fact]
    public void ConfirmedOrder_CannotHaveItemsReplaced()
    {
        var order = NewOrder();
        order.Confirm();
        Assert.Throws<DomainException>(() => order.ReplaceItems([Line()]));
    }

    [Fact]
    public void Cancel_WorksFromPendingAndConfirmed()
    {
        var pending = NewOrder();
        pending.Cancel();
        Assert.Equal(OrderStatus.Cancelled, pending.Status);

        var confirmed = NewOrder();
        confirmed.Confirm();
        confirmed.Cancel();
        Assert.Equal(OrderStatus.Cancelled, confirmed.Status);
    }

    [Fact]
    public void CancelledOrder_IsFrozen()
    {
        var order = NewOrder();
        order.Cancel();
        Assert.Throws<DomainException>(order.Cancel);
        Assert.Throws<DomainException>(order.Confirm);
        Assert.Throws<DomainException>(() => order.ReplaceItems([Line()]));
    }

    [Fact]
    public void EnsureCanBeRemoved_OnlyForCancelled()
    {
        var order = NewOrder();
        Assert.Throws<DomainException>(order.EnsureCanBeRemoved);
        order.Confirm();
        Assert.Throws<DomainException>(order.EnsureCanBeRemoved);
        order.Cancel();
        order.EnsureCanBeRemoved();
    }

    [Fact]
    public void OrderItem_ExposesSubtotal()
    {
        var item = NewOrder(Line(2.5m, 4)).Items.Single();
        Assert.Equal(10m, item.Subtotal.Amount);
        Assert.Equal(4, item.Quantity);
        Assert.Equal(2.5m, item.UnitPrice.Amount);
    }
}
