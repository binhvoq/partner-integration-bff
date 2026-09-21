namespace PartnerIntegration.Domain.ValueObjects;

public sealed record Currency
{
    private static readonly HashSet<string> Iso4217Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AED", "AUD", "BRL", "CAD", "CHF", "CNY", "CZK", "DKK", "EUR", "GBP",
        "HKD", "HUF", "IDR", "INR", "JPY", "KRW", "MXN", "MYR", "NOK", "NZD",
        "PHP", "PLN", "SAR", "SEK", "SGD", "THB", "TRY", "USD", "VND", "ZAR"
    };

    public string Code { get; }

    private Currency(string code) => Code = code;

    public static bool IsSupported(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Iso4217Codes.Contains(code.Trim());

    public static Currency From(string code)
    {
        if (!IsSupported(code))
        {
            throw new ArgumentException(
                $"Currency '{code}' is not a supported ISO 4217 code.",
                nameof(code));
        }

        return new Currency(code.Trim().ToUpperInvariant());
    }

    public override string ToString() => Code;
}
