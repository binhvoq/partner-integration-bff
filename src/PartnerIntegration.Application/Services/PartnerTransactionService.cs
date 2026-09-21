using FluentValidation;
using Microsoft.Extensions.Logging;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;
using PartnerIntegration.Domain.Entities;

namespace PartnerIntegration.Application.Services;

public sealed class PartnerTransactionService : IPartnerTransactionService
{
    private readonly IValidator<CreatePartnerTransactionRequest> _validator;
    private readonly IPartnerVerificationClient _verificationClient;
    private readonly ITransactionQueuePublisher _publisher;
    private readonly ILogger<PartnerTransactionService> _logger;

    public PartnerTransactionService(
        IValidator<CreatePartnerTransactionRequest> validator,
        IPartnerVerificationClient verificationClient,
        ITransactionQueuePublisher publisher,
        ILogger<PartnerTransactionService> logger)
    {
        _validator = validator;
        _verificationClient = verificationClient;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<CreatePartnerTransactionResponse> AcceptAsync(
        CreatePartnerTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Errors);
        }

        var partnerId = request.PartnerId!;
        PartnerVerificationResult verification;

        try
        {
            verification = await _verificationClient.VerifyAsync(partnerId, cancellationToken);
        }
        catch (Exception ex) when (ex is TimeoutException or HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(
                ex,
                "Partner verification failed after retries for {PartnerId}. Returning a controlled error without crashing the request.",
                partnerId);

            throw new PartnerVerificationUnavailableException(partnerId, ex);
        }

        if (!verification.IsVerified)
        {
            throw new PartnerNotVerifiedException(partnerId);
        }

        var transaction = PartnerTransaction.Create(
            partnerId,
            request.TransactionReference!,
            request.Amount!.Value,
            request.Currency!,
            request.Timestamp!.Value);

        var message = PartnerTransactionMessage.From(transaction, verification.PartnerName);

        try
        {
            await _publisher.PublishAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to enqueue transaction {TransactionReference}", transaction.TransactionReference);
            throw new MessagingUnavailableException("The transaction could not be queued for processing.", ex);
        }

        _logger.LogInformation(
            "Accepted partner transaction {TransactionReference} for {PartnerId} and queued it for legacy processing.",
            transaction.TransactionReference,
            partnerId);

        return new CreatePartnerTransactionResponse(
            transaction.Id,
            transaction.TransactionReference,
            transaction.PartnerId,
            "Accepted",
            transaction.ReceivedAt);
    }
}
