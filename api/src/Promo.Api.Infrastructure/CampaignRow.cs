using Promo.Api.Domain;

namespace Promo.Api.Infrastructure;

// Dapper materializes query results into this - a plain, publicly-settable shape - because it
// requires a public parameterless constructor and public setters. Campaign keeps neither (its
// invariants are enforced through its own constructor and Publish()), so this row maps to the
// real entity via its internal reconstitution constructor instead.
internal class CampaignRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BuyerGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public Campaign ToDomain() => new(Id, TenantId, BuyerGroupId, Name, (CampaignStatus)Status, PublishedAtUtc);
}
