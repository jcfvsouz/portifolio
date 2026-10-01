using Components.Messaging;
using Promo.Api.Infrastructure.Notifications;

namespace Promo.Api.Infrastructure.Tests.Notifications;

public class CampaignPublishedNotificationFixture
{
    public Mock<IEventPublisher> EventPublisher { get; private set; } = new();

    public void ConfigureMocks()
    {
        EventPublisher = new();
    }

    public CampaignPublishedNotification NewInstance()
    {
        ConfigureMocks();
        return new(EventPublisher.Object);
    }
}
