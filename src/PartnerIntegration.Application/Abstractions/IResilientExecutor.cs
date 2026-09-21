namespace PartnerIntegration.Application.Abstractions;

public interface IResilientExecutor
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default);
}
