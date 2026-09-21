namespace PartnerIntegration.Application.Contracts;

public sealed record CreatePartnerTransactionResponse(
    string TransactionId,
    string TransactionReference,
    string PartnerId,
    string Status,
    DateTimeOffset AcceptedAt);
