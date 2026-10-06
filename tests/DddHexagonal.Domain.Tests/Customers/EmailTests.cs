using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Domain.Tests.Customers;

public sealed class EmailTests
{
    [Fact]
    public void Create_NormalizesCaseAndWhitespace() =>
        Assert.Equal("john@example.com", Email.Create("  John@Example.COM ").Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("a@b")]
    [InlineData("a b@c.com")]
    [InlineData("@c.com")]
    public void Create_RejectsInvalid(string? raw) => Assert.Throws<DomainException>(() => Email.Create(raw));

    [Fact]
    public void Create_RejectsTooLong() =>
        Assert.Throws<DomainException>(() => Email.Create(new string('a', 250) + "@x.com"));

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(Email.Create("a@b.com"), Email.Create("A@B.com"));
        Assert.Equal("a@b.com", Email.Create("a@b.com").ToString());
    }
}
