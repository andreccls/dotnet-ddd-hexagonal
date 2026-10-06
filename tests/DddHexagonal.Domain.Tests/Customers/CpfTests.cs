using DddHexagonal.Domain.Common;
using DddHexagonal.Domain.Customers;

namespace DddHexagonal.Domain.Tests.Customers;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void Create_AcceptsValidCpf(string raw) => Assert.Equal(11, Cpf.Create(raw).Value.Length);

    [Fact]
    public void Create_StoresOnlyDigitsAndFormatsOnDemand()
    {
        var cpf = Cpf.Create("529.982.247-25");
        Assert.Equal("52998224725", cpf.Value);
        Assert.Equal("529.982.247-25", cpf.Formatted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("111.111.111-11")]   // repeated digits
    [InlineData("529.982.247-24")]   // wrong 2nd check digit
    [InlineData("529.982.247-35")]   // wrong 1st check digit
    [InlineData("abcdefghijk")]
    public void Create_RejectsInvalid(string? raw) => Assert.Throws<DomainException>(() => Cpf.Create(raw));

    [Fact]
    public void Create_AcceptsCpfWhoseCheckDigitsAreZero() =>
        // Both check digits hit the "remainder >= 10 => 0" branch.
        Assert.Equal("10000003700", Cpf.Create("100.000.037-00").Value);
}
