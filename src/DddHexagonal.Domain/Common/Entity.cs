namespace DddHexagonal.Domain.Common;

/// <summary>Base class for objects with identity: two entities are equal when type and Id match.</summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Id must not be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Required by EF Core materialization only.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; private set; }

    public bool Equals(Entity? other) => other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj) => Equals(obj as Entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>Marks the consistency boundary: repositories only load/save aggregate roots.</summary>
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }
}
