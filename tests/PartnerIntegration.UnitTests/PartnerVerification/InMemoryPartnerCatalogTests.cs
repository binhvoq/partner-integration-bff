using PartnerIntegration.Infrastructure.PartnerVerification;

namespace PartnerIntegration.UnitTests.PartnerVerification;

public sealed class InMemoryPartnerCatalogTests
{
    private readonly InMemoryPartnerCatalog _catalog = new();

    [Theory]
    [InlineData("P-1001", "Northwind Payments")]
    [InlineData("p-1002", "Contoso Travel")]
    [InlineData("P-2001", "Fabrikam Logistics")]
    public void Known_partners_are_resolved(string partnerId, string expectedName)
    {
        var found = _catalog.TryGet(partnerId, out var name);

        Assert.True(found);
        Assert.Equal(expectedName, name);
    }

    [Fact]
    public void Unknown_partners_are_not_resolved()
    {
        var found = _catalog.TryGet("P-9999", out var name);

        Assert.False(found);
        Assert.Null(name);
    }
}
