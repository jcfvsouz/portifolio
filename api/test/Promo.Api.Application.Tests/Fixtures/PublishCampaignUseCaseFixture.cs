using Components.Messaging;
using Promo.Api.Domain;

namespace Promo.Api.Application.Tests.Fixtures;

public class PublishCampaignUseCaseFixture
{
    public Mock<ICampaignRepository> CampaignRepository { get; private set; } = new();
    public Mock<IEventPublisher> EventPublisher { get; private set; } = new();

    public void ConfigureMocks()
    {
        CampaignRepository = new();
        EventPublisher = new();
    }

    public PublishCampaignUseCase NewInstance()
    {
        ConfigureMocks();
        return new(CampaignRepository.Object, EventPublisher.Object);
    }

    public void Setup_CampaignRepository_GetById_Result(Campaign? campaign) =>
        CampaignRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(campaign);
}
