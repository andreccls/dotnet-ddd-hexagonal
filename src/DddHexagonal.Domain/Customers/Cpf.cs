using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Customers;

/// <summary>Brazilian individual taxpayer id. Stored as 11 digits, validated with the official check digits.</summary>
public sealed class Cpf : ValueObject
{
    private Cpf(string value) => Value = value;

    public string Value { get; }

    public string Formatted => $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";

    public static Cpf Create(string? raw)
    {
        var digits = new string((raw ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        var allowedChars = (raw ?? string.Empty).All(c => char.IsAsciiDigit(c) || c is '.' or '-' or ' ');
        if (!allowedChars || digits.Length != 11 || digits.Distinct().Count() == 1 || !HasValidCheckDigits(digits))
        {
            throw new DomainException("Document (CPF) is invalid.");
        }

        return new Cpf(digits);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    private static bool HasValidCheckDigits(string digits) =>
        CheckDigit(digits, 9) == digits[9] - '0' && CheckDigit(digits, 10) == digits[10] - '0';

    private static int CheckDigit(string digits, int length)
    {
        var sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += (digits[i] - '0') * (length + 1 - i);
        }

        var result = 11 - (sum % 11);
        return result >= 10 ? 0 : result;
    }
}
