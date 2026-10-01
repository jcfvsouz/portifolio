using Promo.Api.Domain.Entities;
using Promo.Api.Infrastructure.Notifications;

namespace Promo.Api.Infrastructure.Tests.Notifications;

public class CampaignPublishedNotificationTests(CampaignPublishedNotificationFixture fixture) : IClassFixture<CampaignPublishedNotificationFixture>
{
    [Fact]
    public async Task NotifyAsync_publishes_a_CampaignPublished_event_carrying_the_campaign_data()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new Campaign(Guid.NewGuid(), Guid.NewGuid(), "Black Friday");
        campaign.Publish();

        // Act
        await sut.NotifyAsync(campaign);

        // Assert
        fixture.EventPublisher.Verify(p => p.PublishAsync(
            It.Is<CampaignPublished>(e =>
                e.CampaignId == campaign.Id &&
                e.TenantId == campaign.TenantId &&
                e.BuyerGroupId == campaign.BuyerGroupId &&
                e.Name == campaign.Name &&
                e.PublishedAtUtc == campaign.PublishedAtUtc),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
