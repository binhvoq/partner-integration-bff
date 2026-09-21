using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Options;

namespace PartnerIntegration.Api.Authentication;

public sealed class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
}

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiSecurityOptions _security;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiSecurityOptions> security)
        : base(options, logger, encoder)
    {
        _security = security.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (string.IsNullOrWhiteSpace(_security.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key is not configured on the server."));
        }

        if (!Request.Headers.TryGetValue(_security.HeaderName, out var providedKey) ||
            string.IsNullOrWhiteSpace(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {_security.HeaderName} header."));
        }

        if (!CryptographicEquals(_security.ApiKey, providedKey.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "partner-client"),
            new Claim(ClaimTypes.Name, "partner-client"),
            new Claim(ClaimTypes.Role, "Partner")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool CryptographicEquals(string expected, string actual)
    {
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var actualBytes = System.Text.Encoding.UTF8.GetBytes(actual);

        if (expectedBytes.Length != actualBytes.Length)
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
