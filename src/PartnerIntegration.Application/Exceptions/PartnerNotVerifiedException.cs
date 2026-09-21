namespace PartnerIntegration.Application.Exceptions;

public sealed class PartnerNotVerifiedException : Exception
{
    public string PartnerId { get; }

    public PartnerNotVerifiedException(string partnerId)
        : base($"Partner '{partnerId}' is not registered or is not active.")
    {
        PartnerId = partnerId;
    }
}
