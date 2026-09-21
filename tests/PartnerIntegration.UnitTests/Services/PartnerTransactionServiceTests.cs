using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;
using PartnerIntegration.Application.Services;
using PartnerIntegration.Application.Validators;

namespace PartnerIntegration.UnitTests.Services;

public sealed class PartnerTransactionServiceTests
{
    private readonly Mock<IPartnerVerificationClient> _verification = new();
    private readonly Mock<ITransactionQueuePublisher> _publisher = new();
    private readonly PartnerTransactionService _sut;

    public PartnerTransactionServiceTests()
    {
        _sut = new PartnerTransactionService(
            new CreatePartnerTransactionRequestValidator(),
            _verification.Object,
            _publisher.Object,
            NullLogger<PartnerTransactionService>.Instance);
    }

    [Fact]
    public async Task AcceptAsync_queues_transaction_when_partner_is_verified()
    {
        _verification
            .Setup(client => client.VerifyAsync("P-1001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartnerVerificationResult("P-1001", true, "Northwind Payments"));

        var response = await _sut.AcceptAsync(ValidRequest());

        Assert.Equal("Accepted", response.Status);
        Assert.Equal("TXN-99823", response.TransactionReference);
        _publisher.Verify(
            publisher => publisher.PublishAsync(It.Is<PartnerTransactionMessage>(message =>
                message.PartnerId == "P-1001" &&
                message.Amount == 250.00m &&
                message.Currency == "USD"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_throws_validation_exception_for_invalid_payload()
    {
        var request = ValidRequest() with { Amount = 0 };

        await Assert.ThrowsAsync<ValidationException>(() => _sut.AcceptAsync(request));
        _verification.Verify(client => client.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(publisher => publisher.PublishAsync(It.IsAny<PartnerTransactionMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_throws_when_partner_is_not_verified()
    {
        _verification
            .Setup(client => client.VerifyAsync("P-9999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartnerVerificationResult("P-9999", false, null));

        var request = ValidRequest() with { PartnerId = "P-9999" };

        await Assert.ThrowsAsync<PartnerNotVerifiedException>(() => _sut.AcceptAsync(request));
        _publisher.Verify(publisher => publisher.PublishAsync(It.IsAny<PartnerTransactionMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_does_not_crash_when_verification_times_out()
    {
        _verification
            .Setup(client => client.VerifyAsync("P-1001", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("simulated timeout"));

        var exception = await Assert.ThrowsAsync<PartnerVerificationUnavailableException>(() => _sut.AcceptAsync(ValidRequest()));

        Assert.Equal("P-1001", exception.PartnerId);
        _publisher.Verify(publisher => publisher.PublishAsync(It.IsAny<PartnerTransactionMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_does_not_crash_when_verification_returns_http_failure()
    {
        _verification
            .Setup(client => client.VerifyAsync("P-1001", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("502 Bad Gateway"));

        await Assert.ThrowsAsync<PartnerVerificationUnavailableException>(() => _sut.AcceptAsync(ValidRequest()));
    }

    [Fact]
    public async Task AcceptAsync_throws_when_queue_publish_fails()
    {
        _verification
            .Setup(client => client.VerifyAsync("P-1001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PartnerVerificationResult("P-1001", true, "Northwind Payments"));
        _publisher
            .Setup(publisher => publisher.PublishAsync(It.IsAny<PartnerTransactionMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("broker down"));

        await Assert.ThrowsAsync<MessagingUnavailableException>(() => _sut.AcceptAsync(ValidRequest()));
    }

    private static CreatePartnerTransactionRequest ValidRequest() => new()
    {
        PartnerId = "P-1001",
        TransactionReference = "TXN-99823",
        Amount = 250.00m,
        Currency = "USD",
        Timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z")
    };
}
