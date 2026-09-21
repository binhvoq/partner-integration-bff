using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Api.Controllers;

/// <summary>
/// Dummy Partner Verification API hosted in-process to simulate an unreliable upstream dependency.
/// Randomly throws <see cref="TimeoutException"/> 30% of the time.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("internal/v1/partners")]
[Produces("application/json")]
public sealed class PartnerVerificationController : ControllerBase
{
    private readonly ITimeoutFailureInjector _failureInjector;
    private readonly IPartnerCatalog _catalog;

    public PartnerVerificationController(
        ITimeoutFailureInjector failureInjector,
        IPartnerCatalog catalog)
    {
        _failureInjector = failureInjector;
        _catalog = catalog;
    }

    [HttpGet("{partnerId}/verify")]
    [ProducesResponseType(typeof(PartnerVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public ActionResult<PartnerVerificationResult> Verify(string partnerId)
    {
        _failureInjector.MaybeThrowTimeout();

        if (!_catalog.TryGet(partnerId, out var partnerName))
        {
            return NotFound(new PartnerVerificationResult(partnerId, false, null));
        }

        return Ok(new PartnerVerificationResult(partnerId, true, partnerName));
    }
}
