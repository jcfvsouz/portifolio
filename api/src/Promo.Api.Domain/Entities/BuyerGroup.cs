namespace Promo.Api.Domain.Entities;

public class BuyerGroup
{
    public Guid Id { get; internal set; }
    public Guid TenantId { get; internal set; }
    public string Name { get; internal set; } = string.Empty;

    public BuyerGroup(Guid tenantId, string name)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
    }
}
