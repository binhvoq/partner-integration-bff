using PartnerIntegration.Domain.Entities;
using PartnerIntegration.Domain.ValueObjects;

namespace PartnerIntegration.UnitTests.Domain;

public sealed class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData("VND")]
    public void From_accepts_supported_codes(string code)
    {
        var currency = Currency.From(code);

        Assert.Equal(code.ToUpperInvariant(), currency.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDT")]
    public void From_rejects_unsupported_codes(string? code)
    {
        Assert.Throws<ArgumentException>(() => Currency.From(code!));
    }
}

public sealed class PartnerTransactionTests
{
    [Fact]
    public void Create_builds_a_normalized_transaction()
    {
        var timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z");

        var transaction = PartnerTransaction.Create(" P-1001 ", " TXN-99823 ", 250.555m, "usd", timestamp);

        Assert.False(string.IsNullOrWhiteSpace(transaction.Id));
        Assert.Equal("P-1001", transaction.PartnerId);
        Assert.Equal("TXN-99823", transaction.TransactionReference);
        Assert.Equal(250.56m, transaction.Amount);
        Assert.Equal("USD", transaction.Currency.Code);
        Assert.Equal(timestamp, transaction.Timestamp);
    }

    [Fact]
    public void Create_rejects_non_positive_amount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PartnerTransaction.Create("P-1001", "TXN-1", 0, "USD", DateTimeOffset.UtcNow));
    }
}
