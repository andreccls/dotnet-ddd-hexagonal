namespace DddHexagonal.Application.Ports.In;

/// <summary>
/// Inbound port: one use case = one request type = one response type (KISS, no mediator).
/// Driving adapters (HTTP, CLI, message consumers...) depend on this interface only.
/// </summary>
public interface IUseCase<in TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
