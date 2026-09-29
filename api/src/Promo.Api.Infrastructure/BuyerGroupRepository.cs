using Components.SQLRepository;
using Dapper;
using Promo.Api.Application;
using Promo.Api.Domain;

namespace Promo.Api.Infrastructure;

public class BuyerGroupRepository(ISqlRepository sqlRepository) : IBuyerGroupRepository
{
    public async Task<BuyerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, TenantId, Name FROM BuyerGroups WHERE Id = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var row = await sqlRepository.Connection.QuerySingleOrDefaultAsync<BuyerGroupRow>(command);
        return row?.ToDomain();
    }
}
