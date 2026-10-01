using Bogus;
using Promo.Api.Domain.Entities;

namespace Promo.Api.Application.Tests.Builders;

public class CampaignBuilder
{
    private static readonly Faker Faker = new();

    private Guid _tenantId = Guid.NewGuid();
    private Guid _buyerGroupId = Guid.NewGuid();
    private string _name = Faker.Commerce.ProductName();

    public CampaignBuilder WithTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public CampaignBuilder WithBuyerGroup(Guid buyerGroupId)
    {
        _buyerGroupId = buyerGroupId;
        return this;
    }

    public CampaignBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public Campaign Build() => new(_tenantId, _buyerGroupId, _name);
}
