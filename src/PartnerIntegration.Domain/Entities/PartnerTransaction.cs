using PartnerIntegration.Domain.ValueObjects;

namespace PartnerIntegration.Domain.Entities;

public sealed class PartnerTransaction
{
    public string Id { get; }
    public string PartnerId { get; }
    public string TransactionReference { get; }
    public decimal Amount { get; }
    public Currency Currency { get; }
    public DateTimeOffset Timestamp { get; }
    public DateTimeOffset ReceivedAt { get; }

    private PartnerTransaction(
        string id,
        string partnerId,
        string transactionReference,
        decimal amount,
        Currency currency,
        DateTimeOffset timestamp,
        DateTimeOffset receivedAt)
    {
        Id = id;
        PartnerId = partnerId;
        TransactionReference = transactionReference;
        Amount = amount;
        Currency = currency;
        Timestamp = timestamp;
        ReceivedAt = receivedAt;
    }

    public static PartnerTransaction Create(
        string partnerId,
        string transactionReference,
        decimal amount,
        string currencyCode,
        DateTimeOffset timestamp,
        DateTimeOffset? receivedAt = null)
    {
        if (string.IsNullOrWhiteSpace(partnerId))
        {
            throw new ArgumentException("Partner id is required.", nameof(partnerId));
        }

        if (string.IsNullOrWhiteSpace(transactionReference))
        {
            throw new ArgumentException("Transaction reference is required.", nameof(transactionReference));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        return new PartnerTransaction(
            id: Guid.NewGuid().ToString("N"),
            partnerId: partnerId.Trim(),
            transactionReference: transactionReference.Trim(),
            amount: decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            currency: Currency.From(currencyCode),
            timestamp: timestamp,
            receivedAt: receivedAt ?? DateTimeOffset.UtcNow);
    }
}
