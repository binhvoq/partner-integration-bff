namespace PartnerIntegration.Application.Options;

public sealed class ApiSecurityOptions
{
    public const string SectionName = "Security";

    public string ApiKey { get; set; } = string.Empty;

    public string HeaderName { get; set; } = "X-Api-Key";
}
