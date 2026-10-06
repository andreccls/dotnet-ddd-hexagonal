using System.Text.RegularExpressions;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Products;

/// <summary>Stock keeping unit: 3 to 32 chars, letters/digits/hyphen, always upper case.</summary>
public sealed partial class Sku : ValueObject
{
    private Sku(string value) => Value = value;

    public string Value { get; }

    public static Sku Create(string? raw)
    {
        var value = raw?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(value) || !Pattern().IsMatch(value))
        {
            throw new DomainException("SKU must have 3 to 32 characters (letters, digits or hyphen).");
        }

        return new Sku(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex("^[A-Z0-9-]{3,32}$")]
    private static partial Regex Pattern();
}
