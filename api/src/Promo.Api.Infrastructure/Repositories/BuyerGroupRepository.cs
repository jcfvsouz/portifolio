using Components.SQLRepository;
using Dapper;
using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Repositories;
using Promo.Api.Infrastructure.Persistence;

namespace Promo.Api.Infrastructure.Repositories;

public class BuyerGroupRepository(ISqlRepository sqlRepository) : IBuyerGroupRepository
{
    public async Task<BuyerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, TenantId, Name FROM BuyerGroups WHERE Id = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var row = await sqlRepository.Connection.QuerySingleOrDefaultAsync<BuyerGroupRow>(command);
        return row is null ? null : (BuyerGroup)row;
    }
}
