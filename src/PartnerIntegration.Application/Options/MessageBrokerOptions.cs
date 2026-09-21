namespace PartnerIntegration.Application.Options;

public sealed class MessageBrokerOptions
{
    public const string SectionName = "MessageBroker";

    public string Provider { get; set; } = "RabbitMQ";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string QueueName { get; set; } = "partner.transactions";
}
