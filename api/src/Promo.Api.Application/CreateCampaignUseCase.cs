using Components.Result;
using FluentValidation;
using Promo.Api.Domain;

namespace Promo.Api.Application;

public class CreateCampaignUseCase(
    ICampaignRepository campaignRepository,
    IBuyerGroupRepository buyerGroupRepository,
    IValidator<CreateCampaignRequest> validator)
{
    public async Task<Result<Guid>> ExecuteAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<Guid>.Error(validation.Errors);

        var buyerGroup = await buyerGroupRepository.GetByIdAsync(request.BuyerGroupId, cancellationToken);
        if (buyerGroup is null)
            return Result<Guid>.Error($"Buyer group '{request.BuyerGroupId}' was not found.");

        var campaign = new Campaign(buyerGroup.TenantId, buyerGroup.Id, request.Name);
        await campaignRepository.AddAsync(campaign, cancellationToken);

        return Result<Guid>.Ok(campaign.Id);
    }
}
