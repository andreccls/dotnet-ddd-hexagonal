namespace DddHexagonal.Application.Ports.Out;

/// <summary>Outbound port: commits every change made through the repositories atomically.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
