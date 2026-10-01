using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Enums;

namespace Promo.Api.Infrastructure.Persistence;

internal class CampaignRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid BuyerGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public static explicit operator Campaign(CampaignRow row) =>
        new(row.TenantId, row.BuyerGroupId, row.Name)
        {
            Id = row.Id,
            Status = (CampaignStatus)row.Status,
            PublishedAtUtc = row.PublishedAtUtc
        };
}
