namespace Promo.Api.Domain;

public class BuyerGroup
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private BuyerGroup()
    {
    }

    public BuyerGroup(Guid tenantId, string name)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
    }
}
