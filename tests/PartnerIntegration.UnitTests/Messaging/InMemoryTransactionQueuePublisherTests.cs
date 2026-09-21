using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PartnerIntegration.UnitTests.Messaging;

public sealed class InMemoryTransactionQueuePublisherTests
{
    [Fact]
    public async Task PublishAsync_stores_the_message()
    {
        var publisher = new InMemoryTransactionQueuePublisher(NullLogger<InMemoryTransactionQueuePublisher>.Instance);
        var message = new PartnerTransactionMessage(
            "id-1",
            "P-1001",
            "Northwind Payments",
            "TXN-99823",
            250.00m,
            "USD",
            DateTimeOffset.Parse("2024-05-10T14:30:00Z"),
            DateTimeOffset.UtcNow);

        await publisher.PublishAsync(message);

        Assert.Single(publisher.Messages);
        Assert.Equal("TXN-99823", publisher.Messages.First().TransactionReference);
    }
}
