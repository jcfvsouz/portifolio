using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Repositories;
using Promo.Api.Domain.Services;

namespace Promo.Api.Domain.Tests.Fixtures;

public class PublishCampaignServiceFixture
{
    public Mock<ICampaignRepository> CampaignRepository { get; private set; } = new();

    public void ConfigureMocks()
    {
        CampaignRepository = new();
    }

    public PublishCampaignService NewInstance()
    {
        ConfigureMocks();
        return new(CampaignRepository.Object);
    }

    public void Setup_CampaignRepository_GetById_Result(Campaign? campaign) =>
        CampaignRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
}
