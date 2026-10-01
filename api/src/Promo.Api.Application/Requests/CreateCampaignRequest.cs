namespace Promo.Api.Application.Requests;

public record CreateCampaignRequest(string Name, Guid BuyerGroupId);
