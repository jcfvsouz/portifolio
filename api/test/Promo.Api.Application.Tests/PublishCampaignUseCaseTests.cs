using Promo.Api.Application.Tests.Builders;
using Promo.Api.Application.Tests.Fixtures;
using Promo.Api.Domain;

namespace Promo.Api.Application.Tests;

public class PublishCampaignUseCaseTests(PublishCampaignUseCaseFixture fixture) : IClassFixture<PublishCampaignUseCaseFixture>
{
    [Fact]
    public async Task ExecuteAsync_fails_when_the_campaign_does_not_exist()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_CampaignRepository_GetById_Result(null);

        // Act
        var result = await sut.ExecuteAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        fixture.EventPublisher.Verify(p => p.PublishAsync(It.IsAny<CampaignPublished>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_publishes_the_campaign_persists_it_and_raises_CampaignPublished()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new CampaignBuilder().Build();
        fixture.Setup_CampaignRepository_GetById_Result(campaign);

        // Act
        var result = await sut.ExecuteAsync(campaign.Id);

        // Assert
        result.Success.Should().BeTrue();
        fixture.CampaignRepository.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
        fixture.EventPublisher.Verify(p => p.PublishAsync(
            It.Is<CampaignPublished>(e => e.CampaignId == campaign.Id && e.TenantId == campaign.TenantId && e.BuyerGroupId == campaign.BuyerGroupId && e.Name == campaign.Name),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_fails_without_republishing_when_the_campaign_is_already_published()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new CampaignBuilder().Build();
        campaign.Publish();
        fixture.Setup_CampaignRepository_GetById_Result(campaign);

        // Act
        var result = await sut.ExecuteAsync(campaign.Id);

        // Assert
        result.Success.Should().BeFalse();
        fixture.CampaignRepository.Verify(r => r.UpdateAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.EventPublisher.Verify(p => p.PublishAsync(It.IsAny<CampaignPublished>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
