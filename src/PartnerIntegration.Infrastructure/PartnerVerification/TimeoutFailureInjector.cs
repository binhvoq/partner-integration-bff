using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Options;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class TimeoutFailureInjector : ITimeoutFailureInjector
{
    private readonly IChanceGenerator _chanceGenerator;
    private readonly PartnerVerificationOptions _options;
    private readonly ILogger<TimeoutFailureInjector> _logger;

    public TimeoutFailureInjector(
        IChanceGenerator chanceGenerator,
        IOptions<PartnerVerificationOptions> options,
        ILogger<TimeoutFailureInjector> logger)
    {
        _chanceGenerator = chanceGenerator;
        _options = options.Value;
        _logger = logger;
    }

    public void MaybeThrowTimeout()
    {
        var chance = _chanceGenerator.NextChance();
        if (chance < _options.TimeoutProbability)
        {
            _logger.LogWarning(
                "Simulating Partner Verification API timeout (chance {Chance:F2} < {Probability:F2}).",
                chance,
                _options.TimeoutProbability);

            throw new TimeoutException("Partner Verification API timed out while validating the partner.");
        }
    }
}
