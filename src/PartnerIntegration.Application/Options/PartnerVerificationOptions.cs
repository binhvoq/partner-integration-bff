namespace PartnerIntegration.Application.Options;

public sealed class PartnerVerificationOptions
{
    public const string SectionName = "PartnerVerification";

    public string BaseUrl { get; set; } = "http://localhost:5263";

    public double TimeoutProbability { get; set; } = 0.30;

    public int MaxRetryAttempts { get; set; } = 3;

    public int RetryDelayMilliseconds { get; set; } = 200;

    public int RequestTimeoutSeconds { get; set; } = 5;

    public string VerifyPathTemplate { get; set; } = "/internal/v1/partners/{0}/verify";
}
