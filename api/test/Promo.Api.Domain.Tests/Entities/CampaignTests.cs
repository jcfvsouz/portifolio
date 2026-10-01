using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Enums;

namespace Promo.Api.Domain.Tests.Entities;

public class CampaignTests
{
    [Fact]
    public void New_campaign_starts_as_draft()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var buyerGroupId = Guid.NewGuid();

        // Act
        var campaign = new Campaign(tenantId, buyerGroupId, "Black Friday");

        // Assert
        campaign.Status.Should().Be(CampaignStatus.Draft);
        campaign.PublishedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Publish_moves_a_draft_campaign_to_published()
    {
        // Arrange
        var campaign = new Campaign(Guid.NewGuid(), Guid.NewGuid(), "Black Friday");

        // Act
        var result = campaign.Publish();

        // Assert
        result.Success.Should().BeTrue();
        campaign.Status.Should().Be(CampaignStatus.Published);
        campaign.PublishedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Publish_fails_when_the_campaign_is_already_published()
    {
        // Arrange
        var campaign = new Campaign(Guid.NewGuid(), Guid.NewGuid(), "Black Friday");
        campaign.Publish();

        // Act
        var result = campaign.Publish();

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Campaign is already published.");
    }
}
