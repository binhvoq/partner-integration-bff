using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Infrastructure.Messaging;
using PartnerIntegration.UnitTests.Fakes;

namespace PartnerIntegration.UnitTests.Api;

[Collection("api")]
public sealed class DummyPartnerVerificationEndpointTests
{
    [Fact]
    public async Task Verify_returns_known_partner()
    {
        await using var factory = new DummyApiFactory(chance: 0.99);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/internal/v1/partners/P-1001/verify");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PartnerVerificationResult>();
        body.Should().NotBeNull();
        body!.IsVerified.Should().BeTrue();
        body.PartnerName.Should().Be("Northwind Payments");
    }

    [Fact]
    public async Task Verify_returns_404_for_unknown_partner()
    {
        await using var factory = new DummyApiFactory(chance: 0.99);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/internal/v1/partners/P-9999/verify");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Verify_returns_504_when_dummy_api_times_out()
    {
        await using var factory = new DummyApiFactory(chance: 0.01);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/internal/v1/partners/P-1001/verify");

        response.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("timed out");
    }

    private sealed class DummyApiFactory : WebApplicationFactory<Program>
    {
        private readonly double _chance;

        public DummyApiFactory(double chance) => _chance = chance;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Security:ApiKey", "test-api-key");
            builder.UseSetting("MessageBroker:Provider", "InMemory");
            builder.UseSetting("PartnerVerification:TimeoutProbability", "0.30");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IChanceGenerator>();
                services.RemoveAll<ITransactionQueuePublisher>();
                services.AddSingleton<IChanceGenerator>(new FixedChanceGenerator(_chance));
                services.AddSingleton<ITransactionQueuePublisher, InMemoryTransactionQueuePublisher>();
            });
        }
    }
}
