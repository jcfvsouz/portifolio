using Promo.Api.Application.Requests;
using Promo.Api.Application.UseCases;

namespace Promo.Api.Endpoints;

public static class CampaignEndpoints
{
    public static IEndpointRouteBuilder MapCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/campaigns", CreateCampaignAsync).RequireAuthorization();
        app.MapPost("/campaigns/{id:guid}/publish", PublishCampaignAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateCampaignAsync(CreateCampaignRequest request, CreateCampaignUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(request, cancellationToken);
        return result.Success
            ? Results.Created($"/campaigns/{result.Content}", new { id = result.Content })
            : Results.BadRequest(new { error = result.ErrorMessage });
    }

    private static async Task<IResult> PublishCampaignAsync(Guid id, PublishCampaignUseCase useCase, CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(id, cancellationToken);
        return result.Success
            ? Results.Ok()
            : Results.BadRequest(new { error = result.ErrorMessage });
    }
}
