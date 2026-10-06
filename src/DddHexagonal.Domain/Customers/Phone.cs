using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Customers;

/// <summary>Optional contact phone. Keeps only digits (10 to 15, DDD/country code included).</summary>
public sealed class Phone : ValueObject
{
    private Phone(string value) => Value = value;

    public string Value { get; }

    public static Phone Create(string? raw)
    {
        var digits = new string((raw ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        var allowedChars = (raw ?? string.Empty).All(c => char.IsAsciiDigit(c) || c is '+' or '-' or ' ' or '(' or ')');
        if (!allowedChars || digits.Length is < 10 or > 15)
        {
            throw new DomainException("Phone is invalid.");
        }

        return new Phone(digits);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
