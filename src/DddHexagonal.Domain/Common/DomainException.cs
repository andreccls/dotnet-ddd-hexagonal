namespace DddHexagonal.Domain.Common;

/// <summary>
/// A business rule was violated. The API maps it to HTTP 400.
/// Subclasses refine the meaning (see <see cref="NotFoundException"/>, <see cref="ConflictException"/>).
/// </summary>
public class DomainException(string message) : Exception(message);

/// <summary>The requested aggregate does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string message) : DomainException(message)
{
    public static NotFoundException For(string entity, Guid id) => new($"{entity} '{id}' was not found.");
}

/// <summary>The operation conflicts with the current state (e.g. duplicated e-mail). Mapped to HTTP 409.</summary>
public sealed class ConflictException(string message) : DomainException(message);
