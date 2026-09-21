using PartnerIntegration.Application.Abstractions;

namespace PartnerIntegration.UnitTests.Fakes;

public sealed class FixedChanceGenerator : IChanceGenerator
{
    private readonly double _chance;

    public FixedChanceGenerator(double chance) => _chance = chance;

    public double NextChance() => _chance;
}
