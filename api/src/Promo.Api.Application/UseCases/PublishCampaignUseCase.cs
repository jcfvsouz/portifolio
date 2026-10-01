using Components.Result;
using Promo.Api.Application.Notifications;
using Promo.Api.Domain.Services;

namespace Promo.Api.Application.UseCases;

public class PublishCampaignUseCase(IPublishCampaignService publishCampaignService, ICampaignPublishedNotifier notifier)
{
    public async Task<Result> ExecuteAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var result = await publishCampaignService.PublishAsync(campaignId, cancellationToken);
        if (!result.Success)
            return Result.Error(result.ErrorMessage);

        await notifier.NotifyAsync(result.Content!, cancellationToken);

        return Result.Ok();
    }
}
