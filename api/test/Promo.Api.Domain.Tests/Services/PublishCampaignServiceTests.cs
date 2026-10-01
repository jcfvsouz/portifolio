using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Tests.Fixtures;

namespace Promo.Api.Domain.Tests.Services;

public class PublishCampaignServiceTests(PublishCampaignServiceFixture fixture) : IClassFixture<PublishCampaignServiceFixture>
{
    [Fact]
    public async Task PublishAsync_fails_when_the_campaign_does_not_exist()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_CampaignRepository_GetById_Result(null);

        // Act
        var result = await sut.PublishAsync(Guid.NewGuid());

        // Assert
        result.Success.Should().BeFalse();
        fixture.CampaignRepository.Verify(r => r.UpdateAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_publishes_and_persists_a_draft_campaign()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new Campaign(Guid.NewGuid(), Guid.NewGuid(), "Black Friday");
        fixture.Setup_CampaignRepository_GetById_Result(campaign);

        // Act
        var result = await sut.PublishAsync(campaign.Id);

        // Assert
        result.Success.Should().BeTrue();
        result.Content.Should().BeSameAs(campaign);
        fixture.CampaignRepository.Verify(r => r.UpdateAsync(campaign, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_fails_without_persisting_when_the_campaign_is_already_published()
    {
        // Arrange
        var sut = fixture.NewInstance();
        var campaign = new Campaign(Guid.NewGuid(), Guid.NewGuid(), "Black Friday");
        campaign.Publish();
        fixture.Setup_CampaignRepository_GetById_Result(campaign);

        // Act
        var result = await sut.PublishAsync(campaign.Id);

        // Assert
        result.Success.Should().BeFalse();
        fixture.CampaignRepository.Verify(r => r.UpdateAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
