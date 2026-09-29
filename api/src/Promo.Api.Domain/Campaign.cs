using Components.Result;

namespace Promo.Api.Domain;

public class Campaign
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid BuyerGroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public CampaignStatus Status { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    private Campaign()
    {
    }

    public Campaign(Guid tenantId, Guid buyerGroupId, string name)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        BuyerGroupId = buyerGroupId;
        Name = name;
        Status = CampaignStatus.Draft;
    }

    public Result Publish()
    {
        if (Status == CampaignStatus.Published)
            return Result.Error("Campaign is already published.");

        Status = CampaignStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;
        return Result.Ok();
    }
}
