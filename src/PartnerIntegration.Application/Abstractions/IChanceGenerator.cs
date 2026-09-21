namespace PartnerIntegration.Application.Abstractions;

public interface IChanceGenerator
{
    /// <summary>
    /// Returns a value in the range [0.0, 1.0).
    /// </summary>
    double NextChance();
}
