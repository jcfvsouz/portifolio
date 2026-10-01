using Bogus;
using Promo.Api.Domain.Entities;

namespace Promo.Api.Application.Tests.Builders;

public class BuyerGroupBuilder
{
    private static readonly Faker Faker = new();

    private Guid _tenantId = Guid.NewGuid();
    private string _name = Faker.Company.CompanyName();

    public BuyerGroupBuilder WithTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public BuyerGroupBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public BuyerGroup Build() => new(_tenantId, _name);
}
