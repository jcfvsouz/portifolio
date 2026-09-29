using FluentValidation.Results;
using Promo.Api.Application.Tests.Builders;
using Promo.Api.Application.Tests.Fixtures;
using Promo.Api.Domain;

namespace Promo.Api.Application.Tests;

public class CreateCampaignUseCaseTests(CreateCampaignUseCaseFixture fixture) : IClassFixture<CreateCampaignUseCaseFixture>
{
    [Fact]
    public async Task ExecuteAsync_fails_when_validation_fails()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_Validator_ValidateAsync_Result(new([new ValidationFailure("Name", "Name is required")]));

        // Act
        var result = await sut.ExecuteAsync(new CreateCampaignRequestBuilder().Build());

        // Assert
        result.Success.Should().BeFalse();
        fixture.BuyerGroupRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.CampaignRepository.Verify(r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_fails_when_the_buyer_group_does_not_exist()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_Validator_ValidateAsync_Result(new());
        fixture.Setup_BuyerGroupRepository_GetById_Result(null);

        // Act
        var result = await sut.ExecuteAsync(new CreateCampaignRequestBuilder().Build());

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("was not found");
        fixture.CampaignRepository.Verify(r => r.AddAsync(It.IsAny<Campaign>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_creates_and_persists_the_campaign_under_the_buyer_group_tenant()
    {
        // Arrange
        var sut = fixture.NewInstance();
        fixture.Setup_Validator_ValidateAsync_Result(new());
        var buyerGroup = new BuyerGroupBuilder().Build();
        fixture.Setup_BuyerGroupRepository_GetById_Result(buyerGroup);
        var request = new CreateCampaignRequestBuilder().ForBuyerGroup(buyerGroup.Id).Build();

        // Act
        var result = await sut.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        fixture.CampaignRepository.Verify(r => r.AddAsync(
            It.Is<Campaign>(c => c.Name == request.Name && c.BuyerGroupId == buyerGroup.Id && c.TenantId == buyerGroup.TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
