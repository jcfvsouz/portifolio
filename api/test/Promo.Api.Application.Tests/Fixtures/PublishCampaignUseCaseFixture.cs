using Components.Result;
using Promo.Api.Application.Notifications;
using Promo.Api.Application.UseCases;
using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Services;

namespace Promo.Api.Application.Tests.Fixtures;

public class PublishCampaignUseCaseFixture
{
    public Mock<IPublishCampaignService> PublishCampaignService { get; private set; } = new();
    public Mock<ICampaignPublishedNotifier> Notifier { get; private set; } = new();

    public void ConfigureMocks()
    {
        PublishCampaignService = new();
        Notifier = new();
    }

    public PublishCampaignUseCase NewInstance()
    {
        ConfigureMocks();
        return new(PublishCampaignService.Object, Notifier.Object);
    }

    public void Setup_PublishCampaignService_Publish_Result(Result<Campaign> result) =>
        PublishCampaignService
            .Setup(s => s.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
}
