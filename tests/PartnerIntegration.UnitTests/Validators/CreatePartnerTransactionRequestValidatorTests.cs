using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Validators;

namespace PartnerIntegration.UnitTests.Validators;

public sealed class CreatePartnerTransactionRequestValidatorTests
{
    private readonly CreatePartnerTransactionRequestValidator _validator = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _validator.Validate(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PartnerId_is_required(string? partnerId)
    {
        var request = ValidRequest() with { PartnerId = partnerId };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.PartnerId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TransactionReference_is_required(string? transactionReference)
    {
        var request = ValidRequest() with { TransactionReference = transactionReference };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.TransactionReference));
    }

    [Fact]
    public void Amount_is_required()
    {
        var request = ValidRequest() with { Amount = null };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "amount is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-250.5)]
    public void Amount_must_be_greater_than_zero(decimal amount)
    {
        var request = ValidRequest() with { Amount = amount };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "amount must be greater than 0.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Currency_is_required(string? currency)
    {
        var request = ValidRequest() with { Currency = currency };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(request.Currency));
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("XXX")]
    [InlineData("usd1")]
    public void Currency_must_be_supported_iso_code(string currency)
    {
        var request = ValidRequest() with { Currency = currency };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("ISO 4217"));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData("VND")]
    public void Supported_currencies_are_accepted(string currency)
    {
        var request = ValidRequest() with { Currency = currency };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Timestamp_is_required()
    {
        var request = ValidRequest() with { Timestamp = null };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == "timestamp is required.");
    }

    [Fact]
    public void Timestamp_cannot_be_far_in_the_future()
    {
        var request = ValidRequest() with { Timestamp = DateTimeOffset.UtcNow.AddHours(1) };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("future"));
    }

    [Fact]
    public void All_required_field_failures_are_returned_together()
    {
        var result = _validator.Validate(new CreatePartnerTransactionRequest());

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePartnerTransactionRequest.PartnerId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePartnerTransactionRequest.TransactionReference));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePartnerTransactionRequest.Amount));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePartnerTransactionRequest.Currency));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePartnerTransactionRequest.Timestamp));
    }

    private static CreatePartnerTransactionRequest ValidRequest() => new()
    {
        PartnerId = "P-1001",
        TransactionReference = "TXN-99823",
        Amount = 250.00m,
        Currency = "USD",
        Timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z")
    };
}
