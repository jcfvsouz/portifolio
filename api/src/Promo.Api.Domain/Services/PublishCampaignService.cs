using Components.Result;
using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Repositories;

namespace Promo.Api.Domain.Services;

public class PublishCampaignService(ICampaignRepository campaignRepository) : IPublishCampaignService
{
    public async Task<Result<Campaign>> PublishAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await campaignRepository.GetByIdAsync(campaignId, cancellationToken);
        if (campaign is null)
            return Result<Campaign>.Error($"Campaign '{campaignId}' was not found.");

        var publishResult = campaign.Publish();
        if (!publishResult.Success)
            return Result<Campaign>.Error(publishResult.ErrorMessage);

        await campaignRepository.UpdateAsync(campaign, cancellationToken);

        return Result<Campaign>.Ok(campaign);
    }
}
