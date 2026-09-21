using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Infrastructure.Messaging;

namespace PartnerIntegration.UnitTests.Api;

[Collection("api")]
public sealed class PartnerTransactionsEndpointTests : IClassFixture<PartnerApiFactory>
{
    private readonly PartnerApiFactory _factory;

    public PartnerTransactionsEndpointTests(PartnerApiFactory factory)
    {
        _factory = factory;
        _factory.VerificationClient.Handler = (partnerId, _) =>
            Task.FromResult(new PartnerVerificationResult(partnerId, true, "Northwind Payments"));
    }

    [Fact]
    public async Task Post_without_api_key_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_with_invalid_api_key_returns_401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_with_invalid_payload_returns_400_problem_details()
    {
        var client = _factory.CreateAuthenticatedClient();
        var payload = ValidPayload() with { Amount = 0 };

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("amount must be greater than 0");
        body.Should().Contain("traceId");
    }

    [Fact]
    public async Task Post_with_unknown_partner_returns_422()
    {
        _factory.VerificationClient.Handler = (partnerId, _) =>
            Task.FromResult(new PartnerVerificationResult(partnerId, false, null));
        var client = _factory.CreateAuthenticatedClient();
        var payload = ValidPayload() with { PartnerId = "P-9999" };

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", payload);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not registered");
    }

    [Fact]
    public async Task Post_returns_503_when_verification_is_unavailable_after_retries()
    {
        _factory.VerificationClient.Handler = (_, _) => throw new TimeoutException("still timing out");
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("temporarily unavailable");
    }

    [Fact]
    public async Task Post_accepts_valid_transaction_and_publishes_to_queue()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/v1/partner/transactions", ValidPayload());

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var payload = await response.Content.ReadFromJsonAsync<CreatePartnerTransactionResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("Accepted");
        payload.TransactionReference.Should().Be("TXN-99823");

        var publisher = (InMemoryTransactionQueuePublisher)_factory.Services.GetRequiredService<ITransactionQueuePublisher>();
        publisher.Messages.Should().Contain(message => message.TransactionReference == "TXN-99823");
    }

    [Fact]
    public async Task Health_endpoint_is_anonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static CreatePartnerTransactionRequest ValidPayload() => new()
    {
        PartnerId = "P-1001",
        TransactionReference = "TXN-99823",
        Amount = 250.00m,
        Currency = "USD",
        Timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z")
    };
}
