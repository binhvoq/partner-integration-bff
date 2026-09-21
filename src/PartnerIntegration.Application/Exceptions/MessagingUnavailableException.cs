namespace PartnerIntegration.Application.Exceptions;

public sealed class MessagingUnavailableException : Exception
{
    public MessagingUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
