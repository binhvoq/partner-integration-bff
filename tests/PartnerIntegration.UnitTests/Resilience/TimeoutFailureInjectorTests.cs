using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Options;
using PartnerIntegration.Infrastructure.PartnerVerification;
using PartnerIntegration.UnitTests.Fakes;

namespace PartnerIntegration.UnitTests.Resilience;

public sealed class TimeoutFailureInjectorTests
{
    [Fact]
    public void Throws_timeout_when_chance_is_below_threshold()
    {
        var injector = Create(chance: 0.29, probability: 0.30);

        var exception = Assert.Throws<TimeoutException>(() => injector.MaybeThrowTimeout());

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Does_not_throw_when_chance_is_at_or_above_threshold()
    {
        var injector = Create(chance: 0.30, probability: 0.30);

        injector.MaybeThrowTimeout();
    }

    [Fact]
    public void Never_throws_when_probability_is_zero()
    {
        var injector = Create(chance: 0.0, probability: 0.0);

        injector.MaybeThrowTimeout();
    }

    private static TimeoutFailureInjector Create(double chance, double probability) =>
        new(
            new FixedChanceGenerator(chance),
            Options.Create(new PartnerVerificationOptions { TimeoutProbability = probability }),
            NullLogger<TimeoutFailureInjector>.Instance);
}
