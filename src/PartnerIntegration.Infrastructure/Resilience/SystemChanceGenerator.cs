using PartnerIntegration.Application.Abstractions;

namespace PartnerIntegration.Infrastructure.Resilience;

public sealed class SystemChanceGenerator : IChanceGenerator
{
    public double NextChance() => Random.Shared.NextDouble();
}
