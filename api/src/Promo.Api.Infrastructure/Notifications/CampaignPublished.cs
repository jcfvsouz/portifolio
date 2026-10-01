namespace Promo.Api.Infrastructure.Notifications;

internal record CampaignPublished(
    Guid CampaignId,
    Guid TenantId,
    Guid BuyerGroupId,
    string Name,
    DateTime PublishedAtUtc);
