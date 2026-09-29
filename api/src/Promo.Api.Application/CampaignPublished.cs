namespace Promo.Api.Application;

public record CampaignPublished(
    Guid CampaignId,
    Guid TenantId,
    Guid BuyerGroupId,
    string Name,
    DateTime PublishedAtUtc);
