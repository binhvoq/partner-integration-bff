using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/partner")]
[Produces("application/json")]
public sealed class PartnerTransactionsController : ControllerBase
{
    private readonly IPartnerTransactionService _service;

    public PartnerTransactionsController(IPartnerTransactionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Accepts a partner transaction, verifies the partner, and queues it for legacy processing.
    /// </summary>
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(CreatePartnerTransactionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePartnerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _service.AcceptAsync(request, cancellationToken);
        return Accepted(result);
    }
}
