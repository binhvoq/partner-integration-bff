namespace PartnerIntegration.Application.Contracts;

public sealed record PartnerVerificationResult(
    string PartnerId,
    bool IsVerified,
    string? PartnerName);
