using PartnerIntegration.Domain.Entities;

namespace PartnerIntegration.Application.Contracts;

public sealed record PartnerTransactionMessage(
    string TransactionId,
    string PartnerId,
    string? PartnerName,
    string TransactionReference,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    DateTimeOffset AcceptedAt)
{
    public static PartnerTransactionMessage From(PartnerTransaction transaction, string? partnerName) =>
        new(
            transaction.Id,
            transaction.PartnerId,
            partnerName,
            transaction.TransactionReference,
            transaction.Amount,
            transaction.Currency.Code,
            transaction.Timestamp,
            transaction.ReceivedAt);
}
