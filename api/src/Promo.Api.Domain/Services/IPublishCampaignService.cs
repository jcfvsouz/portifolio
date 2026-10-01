using Components.Result;
using Promo.Api.Domain.Entities;

namespace Promo.Api.Domain.Services;

public interface IPublishCampaignService
{
    Task<Result<Campaign>> PublishAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
