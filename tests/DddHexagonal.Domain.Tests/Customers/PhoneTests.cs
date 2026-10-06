using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Domain.Tests.Customers;

public sealed class PhoneTests
{
    [Theory]
    [InlineData("(31) 99999-8888", "31999998888")]
    [InlineData("+55 31 3333-4444", "553133334444")]
    public void Create_KeepsOnlyDigits(string raw, string expected) => Assert.Equal(expected, Phone.Create(raw).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("1234567890123456")]
    [InlineData("phone")]
    public void Create_RejectsInvalid(string? raw) => Assert.Throws<DomainException>(() => Phone.Create(raw));
}
