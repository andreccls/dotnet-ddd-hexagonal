using DddHexagonal.Domain.Orders;

namespace DddHexagonal.Application.Orders;

public sealed record OrderItemResponse(Guid ProductId, decimal UnitPrice, int Quantity, decimal Subtotal);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTimeOffset CreatedAt,
    decimal Total,
    IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(Order o) =>
        new(
            o.Id,
            o.CustomerId,
            o.Status.ToString(),
            o.CreatedAt,
            o.Total.Amount,
            [.. o.Items.Select(i => new OrderItemResponse(i.ProductId, i.UnitPrice.Amount, i.Quantity, i.Subtotal.Amount))]);
}
