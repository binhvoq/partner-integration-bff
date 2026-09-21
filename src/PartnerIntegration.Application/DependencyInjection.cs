using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Services;
using PartnerIntegration.Application.Validators;

namespace PartnerIntegration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<Contracts.CreatePartnerTransactionRequest>, CreatePartnerTransactionRequestValidator>();
        services.AddScoped<IPartnerTransactionService, PartnerTransactionService>();
        return services;
    }
}
