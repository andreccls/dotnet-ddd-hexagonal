using System.Diagnostics.CodeAnalysis;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Products;

public sealed class Product : AggregateRoot
{
    private const int NameMaxLength = 200;

    private Product(Guid id, string name, Sku sku, Money price, int stock)
        : base(id)
    {
        Name = name;
        Sku = sku;
        Price = price;
        Stock = stock;
    }

    [ExcludeFromCodeCoverage(Justification = "Parameterless constructor used only by EF Core to materialize the entity.")]
    private Product()
    {
        // EF Core
        Name = null!;
        Sku = null!;
        Price = null!;
    }

    public string Name { get; private set; }

    public Sku Sku { get; private set; }

    public Money Price { get; private set; }

    public int Stock { get; private set; }

    public static Product Create(string name, Sku sku, Money price, int stock) =>
        stock < 0
            ? throw new DomainException("Stock must not be negative.")
            : new Product(Guid.NewGuid(), ValidName(name), sku, price, stock);

    public void Update(string name, Sku sku, Money price)
    {
        Name = ValidName(name);
        Sku = sku;
        Price = price;
    }

    /// <summary>Adds (positive) or removes (negative) units. Stock can never go below zero.</summary>
    public void AdjustStock(int delta)
    {
        if (delta == 0)
        {
            throw new DomainException("Stock adjustment must not be zero.");
        }

        if (Stock + delta < 0)
        {
            throw new DomainException($"Insufficient stock for '{Name}': available {Stock}, requested {-delta}.");
        }

        Stock += delta;
    }

    private static string ValidName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > NameMaxLength)
        {
            throw new DomainException($"Name is required and must have at most {NameMaxLength} characters.");
        }

        return trimmed;
    }
}
