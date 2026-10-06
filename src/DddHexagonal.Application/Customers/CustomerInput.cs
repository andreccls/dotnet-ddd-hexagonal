using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Customers;

internal static class CustomerInput
{
    /// <summary>Phone is optional: null or blank means "no phone".</summary>
    public static Phone? ParsePhone(string? raw) => string.IsNullOrWhiteSpace(raw) ? null : Phone.Create(raw);
}
