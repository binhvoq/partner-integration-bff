using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Options;
using PartnerIntegration.Infrastructure.PartnerVerification;
using PartnerIntegration.Infrastructure.Resilience;
using PartnerIntegration.UnitTests.Fakes;

namespace PartnerIntegration.UnitTests.Resilience;

public sealed class PartnerVerificationHttpClientTests
{
    [Fact]
    public async Task Returns_verified_partner_on_success()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Succeed(HttpStatusCode.OK, """{"partnerId":"P-1001","isVerified":true,"partnerName":"Northwind Payments"}""");
        var client = CreateClient(handler);

        var result = await client.VerifyAsync("P-1001");

        Assert.True(result.IsVerified);
        Assert.Equal("Northwind Payments", result.PartnerName);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Treats_not_found_as_unverified_without_retrying()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Fail(HttpStatusCode.NotFound, """{"partnerId":"P-9999","isVerified":false,"partnerName":null}""");
        var client = CreateClient(handler);

        var result = await client.VerifyAsync("P-9999");

        Assert.False(result.IsVerified);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Retries_gateway_timeout_and_then_succeeds()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Fail(HttpStatusCode.GatewayTimeout)
            .Fail(HttpStatusCode.GatewayTimeout)
            .Succeed(HttpStatusCode.OK, """{"partnerId":"P-1001","isVerified":true,"partnerName":"Northwind Payments"}""");
        var client = CreateClient(handler, maxRetryAttempts: 3);

        var result = await client.VerifyAsync("P-1001");

        Assert.True(result.IsVerified);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task Retries_server_errors_then_throws_when_retries_are_exhausted()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Fail(HttpStatusCode.InternalServerError)
            .Fail(HttpStatusCode.InternalServerError)
            .Fail(HttpStatusCode.InternalServerError)
            .Fail(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler, maxRetryAttempts: 3);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.VerifyAsync("P-1001"));
        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task Converts_http_client_timeout_into_retryable_timeout_exception()
    {
        var handler = new ScriptedHttpMessageHandler()
            .Throw(new TaskCanceledException("timed out", new TimeoutException("inner timeout")))
            .Succeed(HttpStatusCode.OK, """{"partnerId":"P-1001","isVerified":true,"partnerName":"Northwind Payments"}""");
        var client = CreateClient(handler, maxRetryAttempts: 2);

        var result = await client.VerifyAsync("P-1001");

        Assert.True(result.IsVerified);
        Assert.Equal(2, handler.CallCount);
    }

    private static PartnerVerificationHttpClient CreateClient(
        ScriptedHttpMessageHandler handler,
        int maxRetryAttempts = 3)
    {
        var options = new PartnerVerificationOptions
        {
            MaxRetryAttempts = maxRetryAttempts,
            RetryDelayMilliseconds = 1,
            VerifyPathTemplate = "internal/v1/partners/{0}/verify"
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://partners.test/")
        };

        var executor = new PollyResilientExecutor(
            PollyResilientExecutor.CreatePipeline(options),
            NullLogger<PollyResilientExecutor>.Instance);

        return new PartnerVerificationHttpClient(
            httpClient,
            executor,
            Options.Create(options),
            NullLogger<PartnerVerificationHttpClient>.Instance);
    }
}
