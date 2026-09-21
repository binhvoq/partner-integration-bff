namespace PartnerIntegration.Application.Abstractions;

public interface IPartnerCatalog
{
    bool TryGet(string partnerId, out string partnerName);
}
