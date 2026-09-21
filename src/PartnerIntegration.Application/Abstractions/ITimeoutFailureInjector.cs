namespace PartnerIntegration.Application.Abstractions;

public interface ITimeoutFailureInjector
{
    void MaybeThrowTimeout();
}
