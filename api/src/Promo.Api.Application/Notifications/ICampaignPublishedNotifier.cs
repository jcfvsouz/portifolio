using Promo.Api.Domain.Entities;

namespace Promo.Api.Application.Notifications;

public interface ICampaignPublishedNotifier
{
    Task NotifyAsync(Campaign campaign, CancellationToken cancellationToken = default);
}
