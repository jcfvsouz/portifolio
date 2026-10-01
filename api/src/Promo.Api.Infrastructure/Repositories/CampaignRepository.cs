using Components.SQLRepository;
using Dapper;
using Promo.Api.Domain.Entities;
using Promo.Api.Domain.Repositories;
using Promo.Api.Infrastructure.Persistence;

namespace Promo.Api.Infrastructure.Repositories;

public class CampaignRepository(ISqlRepository sqlRepository) : ICampaignRepository
{
    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, TenantId, BuyerGroupId, Name, Status, PublishedAtUtc FROM Campaigns WHERE Id = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var row = await sqlRepository.Connection.QuerySingleOrDefaultAsync<CampaignRow>(command);
        return row is null ? null : (Campaign)row;
    }

    public async Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Campaigns (Id, TenantId, BuyerGroupId, Name, Status, PublishedAtUtc)
            VALUES (@Id, @TenantId, @BuyerGroupId, @Name, @Status, @PublishedAtUtc)
            """;
        var command = new CommandDefinition(sql, campaign, cancellationToken: cancellationToken);
        await sqlRepository.Connection.ExecuteAsync(command);
    }

    public async Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE Campaigns SET Status = @Status, PublishedAtUtc = @PublishedAtUtc WHERE Id = @Id";
        var command = new CommandDefinition(sql, campaign, cancellationToken: cancellationToken);
        await sqlRepository.Connection.ExecuteAsync(command);
    }
}
