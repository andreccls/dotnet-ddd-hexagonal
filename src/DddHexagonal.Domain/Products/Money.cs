using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Products;

/// <summary>Non-negative monetary amount (single currency, 2 decimals). YAGNI: no currency until needed.</summary>
public sealed class Money : ValueObject
{
    private Money(decimal amount) => Amount = amount;

    public static Money Zero { get; } = new(0m);

    public decimal Amount { get; }

    public static Money Create(decimal amount) =>
        amount < 0 ? throw new DomainException("Amount must not be negative.") : new Money(Math.Round(amount, 2, MidpointRounding.AwayFromZero));

    public Money Add(Money other) => new(Amount + other.Amount);

    public Money Multiply(int factor) =>
        factor < 0 ? throw new DomainException("Factor must not be negative.") : new Money(Amount * factor);

    public override string ToString() => Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }
}
