using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Application.Customers;

public sealed record CustomerResponse(Guid Id, string Name, string Email, string Document, string? Phone, bool Active)
{
    public static CustomerResponse From(Customer c) =>
        new(c.Id, c.Name, c.Email.Value, c.Document.Value, c.Phone?.Value, c.Active);
}
