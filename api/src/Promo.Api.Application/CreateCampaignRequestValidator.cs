using FluentValidation;

namespace Promo.Api.Application;

public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.BuyerGroupId).NotEmpty();
    }
}
