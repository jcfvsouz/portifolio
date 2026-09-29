using Promo.Api.Domain;

namespace Promo.Api.Infrastructure;

// See CampaignRow for why this exists instead of Dapper materializing BuyerGroup directly.
internal class BuyerGroupRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    public BuyerGroup ToDomain() => new(Id, TenantId, Name);
}
