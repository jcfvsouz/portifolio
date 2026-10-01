using FluentValidation;
using FluentValidation.Results;
using Promo.Api.Application.Requests;
using Promo.Api.Application.UseCases;
using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Repositories;

namespace Promo.Api.Application.Tests.Fixtures;

public class CreateCampaignUseCaseFixture
{
    public Mock<ICampaignRepository> CampaignRepository { get; private set; } = new();
    public Mock<IBuyerGroupRepository> BuyerGroupRepository { get; private set; } = new();
    public Mock<IValidator<CreateCampaignRequest>> Validator { get; private set; } = new();

    public void ConfigureMocks()
    {
        CampaignRepository = new();
        BuyerGroupRepository = new();
        Validator = new();
    }

    public CreateCampaignUseCase NewInstance()
    {
        ConfigureMocks();
        return new(CampaignRepository.Object, BuyerGroupRepository.Object, Validator.Object);
    }

    public void Setup_Validator_ValidateAsync_Result(ValidationResult result) =>
        Validator
            .Setup(v => v.ValidateAsync(It.IsAny<CreateCampaignRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

    public void Setup_BuyerGroupRepository_GetById_Result(BuyerGroup? buyerGroup) =>
        BuyerGroupRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(buyerGroup);
}
