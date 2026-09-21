using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Options;
using PartnerIntegration.Infrastructure.Messaging;
using PartnerIntegration.Infrastructure.PartnerVerification;
using PartnerIntegration.Infrastructure.Resilience;

namespace PartnerIntegration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<PartnerVerificationOptions>(configuration.GetSection(PartnerVerificationOptions.SectionName));
        services.Configure<MessageBrokerOptions>(configuration.GetSection(MessageBrokerOptions.SectionName));
        services.Configure<ApiSecurityOptions>(configuration.GetSection(ApiSecurityOptions.SectionName));

        services.AddSingleton<IChanceGenerator, SystemChanceGenerator>();
        services.AddSingleton<ITimeoutFailureInjector, TimeoutFailureInjector>();
        services.AddSingleton<IPartnerCatalog, InMemoryPartnerCatalog>();
        services.AddSingleton<IResilientExecutor, PollyResilientExecutor>();

        services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationHttpClient>((sp, client) =>
            {
                var options = sp.GetRequiredService<IOptions<PartnerVerificationOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(Math.Max(options.RequestTimeoutSeconds, 1));
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });

        var broker = configuration.GetSection(MessageBrokerOptions.SectionName).Get<MessageBrokerOptions>()
                     ?? new MessageBrokerOptions();

        if (string.Equals(broker.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ITransactionQueuePublisher, InMemoryTransactionQueuePublisher>();
        }
        else
        {
            services.AddSingleton<ITransactionQueuePublisher, RabbitMqTransactionPublisher>();
        }

        return services;
    }
}
