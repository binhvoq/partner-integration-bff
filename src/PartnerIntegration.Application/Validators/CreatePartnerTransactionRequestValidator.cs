using FluentValidation;
using PartnerIntegration.Domain.ValueObjects;

namespace PartnerIntegration.Application.Validators;

public sealed class CreatePartnerTransactionRequestValidator : AbstractValidator<Contracts.CreatePartnerTransactionRequest>
{
    public CreatePartnerTransactionRequestValidator()
    {
        RuleFor(x => x.PartnerId)
            .NotEmpty()
            .WithMessage("partnerId is required.")
            .MaximumLength(64)
            .WithMessage("partnerId must be 64 characters or fewer.");

        RuleFor(x => x.TransactionReference)
            .NotEmpty()
            .WithMessage("transactionReference is required.")
            .MaximumLength(128)
            .WithMessage("transactionReference must be 128 characters or fewer.");

        RuleFor(x => x.Amount)
            .NotNull()
            .WithMessage("amount is required.")
            .GreaterThan(0)
            .WithMessage("amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("currency is required.")
            .Must(Currency.IsSupported)
            .WithMessage("currency must be a supported ISO 4217 code.");

        RuleFor(x => x.Timestamp)
            .NotNull()
            .WithMessage("timestamp is required.")
            .Must(value => value is null || value.Value <= DateTimeOffset.UtcNow.AddMinutes(5))
            .WithMessage("timestamp cannot be in the future.");
    }
}
