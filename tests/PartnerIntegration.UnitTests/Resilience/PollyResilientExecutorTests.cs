using Microsoft.Extensions.Logging.Abstractions;
using PartnerIntegration.Application.Options;
using PartnerIntegration.Infrastructure.Resilience;

namespace PartnerIntegration.UnitTests.Resilience;

public sealed class PollyResilientExecutorTests
{
    [Fact]
    public async Task Retries_timeout_exceptions_and_returns_successful_result()
    {
        var attempts = 0;
        var executor = CreateExecutor(maxRetryAttempts: 3);

        var result = await executor.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new TimeoutException($"timeout #{attempts}");
            }

            await Task.CompletedTask;
            return "verified";
        });

        Assert.Equal("verified", result);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Retries_http_request_exceptions()
    {
        var attempts = 0;
        var executor = CreateExecutor(maxRetryAttempts: 2);

        var result = await executor.ExecuteAsync(_ =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new HttpRequestException("502 Bad Gateway");
            }

            return Task.FromResult(true);
        });

        Assert.True(result);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Throws_after_retry_budget_is_exhausted()
    {
        var attempts = 0;
        var executor = CreateExecutor(maxRetryAttempts: 3);

        var exception = await Assert.ThrowsAsync<TimeoutException>(() => executor.ExecuteAsync<string>(_ =>
        {
            attempts++;
            throw new TimeoutException("still down");
        }));

        Assert.Equal("still down", exception.Message);
        Assert.Equal(4, attempts);
    }

    [Fact]
    public async Task Does_not_retry_argument_exceptions()
    {
        var attempts = 0;
        var executor = CreateExecutor(maxRetryAttempts: 5);

        await Assert.ThrowsAsync<ArgumentException>(() => executor.ExecuteAsync<string>(_ =>
        {
            attempts++;
            throw new ArgumentException("bad partner id");
        }));

        Assert.Equal(1, attempts);
    }

    private static PollyResilientExecutor CreateExecutor(int maxRetryAttempts) =>
        new(
            PollyResilientExecutor.CreatePipeline(new PartnerVerificationOptions
            {
                MaxRetryAttempts = maxRetryAttempts,
                RetryDelayMilliseconds = 1
            }),
            NullLogger<PollyResilientExecutor>.Instance);
}
