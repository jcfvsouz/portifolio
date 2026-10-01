using Promo.Api.Application.Requests;
using Promo.Api.Application.Tests.Builders;
using Promo.Api.Application.Validators;

namespace Promo.Api.Application.Tests.Validators;

public class CreateCampaignRequestValidatorTests
{
    private readonly CreateCampaignRequestValidator _validator = new();

    [Fact]
    public async Task Validate_succeeds_for_a_well_formed_request()
    {
        // Arrange
        var request = new CreateCampaignRequestBuilder().Build();

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_fails_when_the_name_is_empty()
    {
        // Arrange
        var request = new CreateCampaignRequestBuilder().WithName(string.Empty).Build();

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCampaignRequest.Name));
    }

    [Fact]
    public async Task Validate_fails_when_the_buyer_group_id_is_empty()
    {
        // Arrange
        var request = new CreateCampaignRequestBuilder().ForBuyerGroup(Guid.Empty).Build();

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCampaignRequest.BuyerGroupId));
    }
}
