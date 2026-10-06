using DddHexagonal.Domain.Common;

namespace DddHexagonal.Domain.Tests.Common;

public sealed class EntityTests
{
    private sealed class Foo(Guid id) : Entity(id);

    private sealed class Bar(Guid id) : Entity(id);

    [Fact]
    public void Entities_WithSameIdAndType_AreEqual()
    {
        var id = Guid.NewGuid();
        Assert.Equal(new Foo(id), new Foo(id));
        Assert.True(new Foo(id).Equals((object)new Foo(id)));
        Assert.Equal(new Foo(id).GetHashCode(), new Foo(id).GetHashCode());
    }

    [Fact]
    public void Entities_WithDifferentIds_AreNotEqual() =>
        Assert.NotEqual(new Foo(Guid.NewGuid()), new Foo(Guid.NewGuid()));

    [Fact]
    public void Entities_OfDifferentTypes_AreNotEqual()
    {
        var id = Guid.NewGuid();
        Assert.False(new Foo(id).Equals(new Bar(id)));
    }

    [Fact]
    public void Entity_IsNotEqualToNull()
    {
        Assert.False(new Foo(Guid.NewGuid()).Equals(null));
        Assert.False(new Foo(Guid.NewGuid()).Equals((object?)null));
    }

    [Fact]
    public void Entity_WithEmptyId_IsRejected() =>
        Assert.Throws<ArgumentException>(() => new Foo(Guid.Empty));
}

public sealed class DomainExceptionTests
{
    [Fact]
    public void NotFound_BuildsMessage() =>
        Assert.Contains("Foo", NotFoundException.For("Foo", Guid.NewGuid()).Message, StringComparison.Ordinal);

    [Fact]
    public void Specializations_AreDomainExceptions()
    {
        Assert.IsAssignableFrom<DomainException>(new NotFoundException("x"));
        Assert.IsAssignableFrom<DomainException>(new ConflictException("x"));
    }
}
