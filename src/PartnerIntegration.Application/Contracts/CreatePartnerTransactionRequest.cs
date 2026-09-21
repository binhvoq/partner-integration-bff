namespace PartnerIntegration.Application.Contracts;

public sealed record CreatePartnerTransactionRequest
{
    public string? PartnerId { get; init; }

    public string? TransactionReference { get; init; }

    public decimal? Amount { get; init; }

    public string? Currency { get; init; }

    public DateTimeOffset? Timestamp { get; init; }
}
