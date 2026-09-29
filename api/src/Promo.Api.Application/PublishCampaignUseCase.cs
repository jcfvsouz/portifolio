using Components.Messaging;
using Components.Result;

namespace Promo.Api.Application;

public class PublishCampaignUseCase(ICampaignRepository campaignRepository, IEventPublisher eventPublisher)
{
    public async Task<Result> ExecuteAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result.Error($"Campaign '{campaignId}' was not found.");

        var publishResult = campaign.Publish();
        if (!publishResult.Success)
            return publishResult;

        await campaignRepository.UpdateAsync(campaign, cancellationToken);

        await eventPublisher.PublishAsync(
            new CampaignPublished(campaign.Id, campaign.TenantId, campaign.BuyerGroupId, campaign.Name, campaign.PublishedAtUtc!.Value),
            cancellationToken: cancellationToken);

        return Result.Ok();
    }
}
