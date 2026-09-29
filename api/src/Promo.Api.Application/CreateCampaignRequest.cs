namespace Promo.Api.Application;

public record CreateCampaignRequest(string Name, Guid BuyerGroupId);
