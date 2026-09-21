namespace PartnerIntegration.Application.Exceptions;

public sealed class PartnerVerificationUnavailableException : Exception
{
    public string PartnerId { get; }

    public PartnerVerificationUnavailableException(string partnerId, Exception innerException)
        : base(
            $"Partner verification is temporarily unavailable for '{partnerId}'. The request was not accepted.",
            innerException)
    {
        PartnerId = partnerId;
    }
}
