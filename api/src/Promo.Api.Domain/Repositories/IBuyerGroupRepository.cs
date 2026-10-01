using Promo.Api.Domain.Entities;

namespace Promo.Api.Domain.Repositories;

public interface IBuyerGroupRepository
{
    Task<BuyerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
