using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Infrastructure.Messaging;

public sealed class InMemoryTransactionQueuePublisher : ITransactionQueuePublisher
{
    private readonly ConcurrentQueue<PartnerTransactionMessage> _messages = new();
    private readonly ILogger<InMemoryTransactionQueuePublisher> _logger;

    public InMemoryTransactionQueuePublisher(ILogger<InMemoryTransactionQueuePublisher> logger)
    {
        _logger = logger;
    }

    public IReadOnlyCollection<PartnerTransactionMessage> Messages => _messages.ToArray();

    public Task PublishAsync(PartnerTransactionMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _messages.Enqueue(message);

        _logger.LogInformation(
            "Queued transaction {TransactionReference} in-memory. Payload: {Payload}",
            message.TransactionReference,
            JsonSerializer.Serialize(message));

        return Task.CompletedTask;
    }
}
