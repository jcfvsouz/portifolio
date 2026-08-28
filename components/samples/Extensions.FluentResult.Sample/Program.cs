using Extensions.FluentResult;
using FluentValidation;
using FluentValidation.Results;

Console.WriteLine("--- Extensions.FluentResult sample ---");
Console.WriteLine();

var validator = new CampaignDtoValidator();

var valid = new CampaignDto("Black Friday", TargetGroupId: 7);
var invalid = new CampaignDto("", TargetGroupId: 0);

Run("Valid campaign", valid);
Run("Invalid campaign", invalid);

void Run(string label, CampaignDto dto)
{
    Console.WriteLine($"{label}: {dto}");

    ValidationResult validation = validator.Validate(dto);

    var withoutContent = validation.ToResult();
    Console.WriteLine($"  ToResult()           -> Success={withoutContent.Success}, ErrorMessage=\"{withoutContent.ErrorMessage}\"");

    var withContent = validation.ToResult($"campaign '{dto.Name}' accepted");
    Console.WriteLine($"  ToResult(content)    -> Success={withContent.Success}, Content={(withContent.Content is null ? "(null)" : $"\"{withContent.Content}\"")}");
    Console.WriteLine();
}

record CampaignDto(string Name, int TargetGroupId);

class CampaignDtoValidator : AbstractValidator<CampaignDto>
{
    public CampaignDtoValidator()
    {
        RuleFor(c => c.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(c => c.TargetGroupId).GreaterThan(0).WithMessage("TargetGroupId must be greater than zero");
    }
}
