using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Options;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class PartnerVerificationHttpClient : IPartnerVerificationClient
{
    private readonly HttpClient _httpClient;
    private readonly IResilientExecutor _resilientExecutor;
    private readonly PartnerVerificationOptions _options;
    private readonly ILogger<PartnerVerificationHttpClient> _logger;

    public PartnerVerificationHttpClient(
        HttpClient httpClient,
        IResilientExecutor resilientExecutor,
        IOptions<PartnerVerificationOptions> options,
        ILogger<PartnerVerificationHttpClient> logger)
    {
        _httpClient = httpClient;
        _resilientExecutor = resilientExecutor;
        _options = options.Value;
        _logger = logger;
    }

    public Task<PartnerVerificationResult> VerifyAsync(
        string partnerId,
        CancellationToken cancellationToken = default)
    {
        return _resilientExecutor.ExecuteAsync(
            token => VerifyOnceAsync(partnerId, token),
            cancellationToken);
    }

    private async Task<PartnerVerificationResult> VerifyOnceAsync(
        string partnerId,
        CancellationToken cancellationToken)
    {
        var path = string.Format(_options.VerifyPathTemplate, Uri.EscapeDataString(partnerId));
        _logger.LogDebug("Calling Partner Verification API at {Path} for {PartnerId}", path, partnerId);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(path, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Partner Verification API request timed out.", ex);
        }

        if (response.StatusCode is HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout)
        {
            throw new TimeoutException("Partner Verification API timed out.");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new PartnerVerificationResult(partnerId, false, null);
        }

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException(
                $"Partner Verification API returned {(int)response.StatusCode} {response.StatusCode}.",
                inner: null,
                statusCode: response.StatusCode);
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PartnerVerificationResult>(cancellationToken);
        if (payload is null)
        {
            throw new HttpRequestException("Partner Verification API returned an empty body.");
        }

        return payload;
    }
}
