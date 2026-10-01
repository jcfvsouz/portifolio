using Bogus;
using Promo.Api.Application.Requests;

namespace Promo.Api.Application.Tests.Builders;

public class CreateCampaignRequestBuilder
{
    private static readonly Faker Faker = new();

    private string _name = Faker.Commerce.ProductName();
    private Guid _buyerGroupId = Guid.NewGuid();

    public CreateCampaignRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateCampaignRequestBuilder ForBuyerGroup(Guid buyerGroupId)
    {
        _buyerGroupId = buyerGroupId;
        return this;
    }

    public CreateCampaignRequest Build() => new(_name, _buyerGroupId);
}
