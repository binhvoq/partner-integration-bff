using PartnerIntegration.Application.Abstractions;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class InMemoryPartnerCatalog : IPartnerCatalog
{
    private static readonly Dictionary<string, string> Partners = new(StringComparer.OrdinalIgnoreCase)
    {
        ["P-1001"] = "Northwind Payments",
        ["P-1002"] = "Contoso Travel",
        ["P-2001"] = "Fabrikam Logistics"
    };

    public bool TryGet(string partnerId, out string partnerName) =>
        Partners.TryGetValue(partnerId, out partnerName!);
}
