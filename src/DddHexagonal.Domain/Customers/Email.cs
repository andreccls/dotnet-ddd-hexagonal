using System.Text.RegularExpressions;
using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Customers;

public sealed partial class Email : ValueObject
{
    private const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Email Create(string? raw)
    {
        var value = raw?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || !Pattern().IsMatch(value))
        {
            throw new DomainException("Email is invalid.");
        }

        return new Email(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Pattern();
}
