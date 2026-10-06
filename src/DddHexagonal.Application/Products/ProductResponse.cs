using DddHexagonal.Domain.Products;

namespace DddHexagonal.Application.Products;

public sealed record ProductResponse(Guid Id, string Name, string Sku, decimal Price, int Stock)
{
    public static ProductResponse From(Product p) => new(p.Id, p.Name, p.Sku.Value, p.Price.Amount, p.Stock);
}
