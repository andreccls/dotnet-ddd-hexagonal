using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Customers;

/// <summary>Aggregate root. State changes only through methods that protect the invariants.</summary>
public sealed class Customer : AggregateRoot
{
    private const int NameMaxLength = 200;

    private Customer(Guid id, string name, Email email, Cpf document, Phone? phone)
        : base(id)
    {
        Name = name;
        Email = email;
        Document = document;
        Phone = phone;
        Active = true;
    }

    private Customer()
    {
        // EF Core
        Name = null!;
        Email = null!;
        Document = null!;
    }

    public string Name { get; private set; }

    public Email Email { get; private set; }

    public Cpf Document { get; private set; }

    public Phone? Phone { get; private set; }

    public bool Active { get; private set; }

    public static Customer Create(string name, Email email, Cpf document, Phone? phone) =>
        new(Guid.NewGuid(), ValidName(name), email, document, phone);

    public void Update(string name, Email email, Cpf document, Phone? phone)
    {
        Name = ValidName(name);
        Email = email;
        Document = document;
        Phone = phone;
    }

    public void Activate()
    {
        if (Active)
        {
            throw new DomainException("Customer is already active.");
        }

        Active = true;
    }

    public void Deactivate()
    {
        if (!Active)
        {
            throw new DomainException("Customer is already inactive.");
        }

        Active = false;
    }

    public void EnsureActive()
    {
        if (!Active)
        {
            throw new DomainException("Customer is inactive.");
        }
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
