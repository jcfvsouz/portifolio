using Components.Result;
using Promo.Api.Application.Tests.Builders;
using Promo.Api.Application.Tests.Fixtures;
using Promo.Api.Domain.Entities;

namespace Promo.Api.Application.Tests.UseCases;

public class PublishCampaignUseCaseTests(PublishCampaignUseCaseFixture fixture) : IClassFixture<PublishCampaignUseCaseFixture>
{
    [Fact]
    public async Task ExecuteAsync_fails_without_notifying_when_the_service_fails()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_PublishCampaignService_Publish_Result(Result<Campaign>.Error("Campaign was not found."));

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        fixture.Notifier.Verify(n => n.NotifyAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_notifies_with_the_published_campaign_when_the_service_succeeds()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new CampaignBuilder().Build();
        campaign.Publish();
        fixture.Setup_PublishCampaignService_Publish_Result(Result<Campaign>.Ok(campaign));

        // Act
        var result = await sut.ExecuteAsync(campaign.Id);

        // Assert
        result.Success.Should().BeTrue();
        fixture.Notifier.Verify(n => n.NotifyAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }
}
