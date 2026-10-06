using DddHexagonal.Application.Ports.Out;
using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Orders;
using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Orders;

public sealed record OrderItemRequest(Guid ProductId, int Quantity);

/// <summary>
/// Cross-aggregate coordination lives in the application layer: an order reserves stock when it is
/// created/changed and gives it back when cancelled. Both aggregates are saved in ONE unit of work.
/// </summary>
internal static class OrderStock
{
    public static async Task<Dictionary<Guid, Product>> LoadProductsAsync(
        IProductRepository products,
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken)
    {
        var distinct = ids.Distinct().ToList();
        var found = (await products.GetByIdsAsync(distinct, cancellationToken)).ToDictionary(p => p.Id);
        foreach (var id in distinct.Where(id => !found.ContainsKey(id)))
        {
            throw NotFoundException.For("Product", id);
        }

        return found;
    }

    public static List<OrderLine> BuildLines(IEnumerable<OrderItemRequest> items, IReadOnlyDictionary<Guid, Product> products) =>
        [.. items.Select(i => new OrderLine(i.ProductId, products[i.ProductId].Price, i.Quantity))];

    public static void Reserve(IEnumerable<OrderLine> lines, IReadOnlyDictionary<Guid, Product> products)
    {
        foreach (var line in lines)
        {
            products[line.ProductId].AdjustStock(-line.Quantity);
        }
    }

    public static void Release(IEnumerable<OrderItem> items, IReadOnlyDictionary<Guid, Product> products)
    {
        foreach (var item in items)
        {
            products[item.ProductId].AdjustStock(item.Quantity);
        }
    }
}
