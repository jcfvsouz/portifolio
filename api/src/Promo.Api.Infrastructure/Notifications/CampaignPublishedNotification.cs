using Components.Messaging;
using Promo.Api.Application.Notifications;
using Promo.Api.Domain.Entities;

namespace Promo.Api.Infrastructure.Notifications;

public class CampaignPublishedNotification(IEventPublisher eventPublisher) : ICampaignPublishedNotifier
{
    public Task NotifyAsync(Campaign campaign, CancellationToken cancellationToken = default) =>
        eventPublisher.PublishAsync(
            new CampaignPublished(campaign.Id, campaign.TenantId, campaign.BuyerGroupId, campaign.Name, campaign.PublishedAtUtc!.Value),
            cancellationToken: cancellationToken);
}
