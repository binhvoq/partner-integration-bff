using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Application.Abstractions;

public interface IPartnerTransactionService
{
    Task<CreatePartnerTransactionResponse> AcceptAsync(
        CreatePartnerTransactionRequest request,
        CancellationToken cancellationToken = default);
}
