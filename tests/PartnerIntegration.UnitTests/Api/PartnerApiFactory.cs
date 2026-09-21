using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Infrastructure.Messaging;

namespace PartnerIntegration.UnitTests.Api;

public sealed class PartnerApiFactory : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key";

    public StubPartnerVerificationClient VerificationClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Security:ApiKey", TestApiKey);
        builder.UseSetting("Security:HeaderName", "X-Api-Key");
        builder.UseSetting("MessageBroker:Provider", "InMemory");
        builder.UseSetting("PartnerVerification:TimeoutProbability", "0");
        builder.UseSetting("PartnerVerification:MaxRetryAttempts", "3");
        builder.UseSetting("PartnerVerification:RetryDelayMilliseconds", "1");
        builder.UseSetting("PartnerVerification:BaseUrl", "http://127.0.0.1");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPartnerVerificationClient>();
            services.RemoveAll<ITransactionQueuePublisher>();
            services.AddSingleton<IPartnerVerificationClient>(VerificationClient);
            services.AddSingleton<ITransactionQueuePublisher, InMemoryTransactionQueuePublisher>();
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}

public sealed class StubPartnerVerificationClient : IPartnerVerificationClient
{
    public Func<string, CancellationToken, Task<PartnerVerificationResult>> Handler { get; set; } =
        (partnerId, _) => Task.FromResult(new PartnerVerificationResult(partnerId, true, "Northwind Payments"));

    public Task<PartnerVerificationResult> VerifyAsync(string partnerId, CancellationToken cancellationToken = default) =>
        Handler(partnerId, cancellationToken);
}
