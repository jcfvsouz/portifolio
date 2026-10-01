using FluentValidation;
using Promo.Api.Application.Requests;

namespace Promo.Api.Application.Validators;

public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.BuyerGroupId).NotEmpty();
    }
}
