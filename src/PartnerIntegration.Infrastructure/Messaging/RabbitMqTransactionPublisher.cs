using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Options;
using RabbitMQ.Client;

namespace PartnerIntegration.Infrastructure.Messaging;

public sealed class RabbitMqTransactionPublisher : ITransactionQueuePublisher, IDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<RabbitMqTransactionPublisher> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqTransactionPublisher(
        IOptions<MessageBrokerOptions> options,
        ILogger<RabbitMqTransactionPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        _connectionFactory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(10)
        };
    }

    public Task PublishAsync(PartnerTransactionMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var channel = GetChannel();
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = message.TransactionId;
        properties.CorrelationId = message.TransactionReference;
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation(
            "Published transaction {TransactionReference} to queue {QueueName}",
            message.TransactionReference,
            _options.QueueName);

        return Task.CompletedTask;
    }

    private IModel GetChannel()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        lock (_sync)
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            _connection?.Dispose();
            _channel?.Dispose();

            _connection = _connectionFactory.CreateConnection("partner-integration-bff");
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.ConfirmSelect();

            return _channel;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
