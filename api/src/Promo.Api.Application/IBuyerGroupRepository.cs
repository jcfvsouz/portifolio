using Promo.Api.Domain;

namespace Promo.Api.Application;

public interface IBuyerGroupRepository
{
    Task<BuyerGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
