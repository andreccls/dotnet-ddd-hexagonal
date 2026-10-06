using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Tests.Common;

public sealed class ValueObjectTests
{
    private sealed class Pair(string? a, int b) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return a;
            yield return b;
        }
    }

    [Fact]
    public void SameComponents_AreEqual()
    {
        Assert.Equal(new Pair("x", 1), new Pair("x", 1));
        Assert.True(new Pair("x", 1).Equals((object)new Pair("x", 1)));
        Assert.Equal(new Pair("x", 1).GetHashCode(), new Pair("x", 1).GetHashCode());
        Assert.Equal(new Pair(null, 1).GetHashCode(), new Pair(null, 1).GetHashCode());
    }

    [Fact]
    public void DifferentComponents_AreNotEqual() => Assert.NotEqual(new Pair("x", 1), new Pair("x", 2));

    [Fact]
    public void NullAndOtherTypes_AreNotEqual()
    {
        Assert.False(new Pair("x", 1).Equals(null));
        Assert.False(new Pair("x", 1).Equals("x"));
    }
}
