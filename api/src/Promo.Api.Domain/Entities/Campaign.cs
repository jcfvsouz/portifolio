using Components.Result;
using Promo.Api.Domain.Enums;

namespace Promo.Api.Domain.Entities;

public class Campaign
{
    public Guid Id { get; internal set; }
    public Guid TenantId { get; internal set; }
    public Guid BuyerGroupId { get; internal set; }
    public string Name { get; internal set; } = string.Empty;
    public CampaignStatus Status { get; internal set; }
    public DateTime? PublishedAtUtc { get; internal set; }

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
