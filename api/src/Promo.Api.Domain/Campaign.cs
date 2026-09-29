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

    // Reconstitutes a campaign from persisted state. Not part of the public creation API -
    // AddAsync always starts a campaign via the constructor above; this exists only for
    // Infrastructure to rebuild the exact persisted state on read.
    internal Campaign(Guid id, Guid tenantId, Guid buyerGroupId, string name, CampaignStatus status, DateTime? publishedAtUtc)
    {
        Id = id;
        TenantId = tenantId;
        BuyerGroupId = buyerGroupId;
        Name = name;
        Status = status;
        PublishedAtUtc = publishedAtUtc;
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
