using Promo.Api.Domain.Entities;

namespace Promo.Api.Infrastructure.Persistence;

internal class BuyerGroupRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;

    public static explicit operator BuyerGroup(BuyerGroupRow row) =>
        new(row.TenantId, row.Name) { Id = row.Id };
}
