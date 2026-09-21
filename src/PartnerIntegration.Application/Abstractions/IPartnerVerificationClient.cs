using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Application.Abstractions;

public interface IPartnerVerificationClient
{
    Task<PartnerVerificationResult> VerifyAsync(string partnerId, CancellationToken cancellationToken = default);
}
