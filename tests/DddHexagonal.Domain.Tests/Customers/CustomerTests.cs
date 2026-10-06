using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Domain.Tests.Customers;

public sealed class CustomerTests
{
    private static Customer NewCustomer(Phone? phone = null) =>
        Customer.Create("Maria Silva", Email.Create("maria@example.com"), Cpf.Create("529.982.247-25"), phone);

    [Fact]
    public void Create_StartsActiveWithGivenData()
    {
        var phone = Phone.Create("31999998888");
        var customer = NewCustomer(phone);

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal("Maria Silva", customer.Name);
        Assert.Equal("maria@example.com", customer.Email.Value);
        Assert.Equal(phone, customer.Phone);
        Assert.True(customer.Active);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsBlankName(string? name) =>
        Assert.Throws<DomainException>(() =>
            Customer.Create(name!, Email.Create("a@b.com"), Cpf.Create("52998224725"), null));

    [Fact]
    public void Create_RejectsTooLongName() =>
        Assert.Throws<DomainException>(() =>
            Customer.Create(new string('a', 201), Email.Create("a@b.com"), Cpf.Create("52998224725"), null));

    [Fact]
    public void Create_TrimsName() =>
        Assert.Equal("Ana", Customer.Create("  Ana ", Email.Create("a@b.com"), Cpf.Create("52998224725"), null).Name);

    [Fact]
    public void Update_ChangesAllFields()
    {
        var customer = NewCustomer();
        customer.Update("Maria S.", Email.Create("new@example.com"), Cpf.Create("11144477735"), Phone.Create("3133334444"));

        Assert.Equal("Maria S.", customer.Name);
        Assert.Equal("new@example.com", customer.Email.Value);
        Assert.Equal("11144477735", customer.Document.Value);
        Assert.NotNull(customer.Phone);
    }

    [Fact]
    public void Update_RejectsBlankName() =>
        Assert.Throws<DomainException>(() =>
            NewCustomer().Update(" ", Email.Create("a@b.com"), Cpf.Create("52998224725"), null));

    [Fact]
    public void Deactivate_ThenActivate_TogglesState()
    {
        var customer = NewCustomer();
        customer.Deactivate();
        Assert.False(customer.Active);
        customer.Activate();
        Assert.True(customer.Active);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws() =>
        Assert.Throws<DomainException>(() => NewCustomer().Activate());

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var customer = NewCustomer();
        customer.Deactivate();
        Assert.Throws<DomainException>(customer.Deactivate);
    }

    [Fact]
    public void EnsureActive_ThrowsWhenInactive()
    {
        var customer = NewCustomer();
        customer.EnsureActive();
        customer.Deactivate();
        Assert.Throws<DomainException>(customer.EnsureActive);
    }

    [Fact]
    public void ExposesNoPublicSetters() =>
        Assert.All(typeof(Customer).GetProperties(), p =>
            Assert.True(p.SetMethod is null || !p.SetMethod.IsPublic, p.Name));
}
