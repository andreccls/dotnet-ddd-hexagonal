using DddHexagonal.Domain.Products;

namespace DddHexagonal.Domain.Orders;

/// <summary>Input for creating/replacing order items. Products are referenced by id only (no object graph across aggregates).</summary>
public sealed record OrderLine(Guid ProductId, Money UnitPrice, int Quantity);
