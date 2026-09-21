using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Options;
using Polly;
using Polly.Retry;

namespace PartnerIntegration.Infrastructure.Resilience;

public sealed class PollyResilientExecutor : IResilientExecutor
{
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<PollyResilientExecutor> _logger;

    public PollyResilientExecutor(
        IOptions<PartnerVerificationOptions> options,
        ILogger<PollyResilientExecutor> logger)
    {
        _logger = logger;
        _pipeline = CreatePipeline(options.Value, logger);
    }

    public PollyResilientExecutor(ResiliencePipeline pipeline, ILogger<PollyResilientExecutor> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async token => await action(token).ConfigureAwait(false),
            cancellationToken);
    }

    public static ResiliencePipeline CreatePipeline(
        PartnerVerificationOptions options,
        ILogger? logger = null)
    {
        var retry = new RetryStrategyOptions
        {
            MaxRetryAttempts = options.MaxRetryAttempts,
            Delay = TimeSpan.FromMilliseconds(Math.Max(options.RetryDelayMilliseconds, 1)),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<TimeoutException>()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>(ex => ex.InnerException is TimeoutException || !ex.CancellationToken.IsCancellationRequested),
            OnRetry = args =>
            {
                logger?.LogWarning(
                    args.Outcome.Exception,
                    "Partner verification attempt {Attempt} failed. Retrying in {RetryDelay}.",
                    args.AttemptNumber + 1,
                    args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retry)
            .Build();
    }
}
