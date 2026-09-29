using Components.Hosting;
using Promo.Api.Application;
using Promo.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.UseStartup<ApplicationModule>();
builder.UseStartup<InfrastructureModule>();

var app = builder.Build();

app.MapPost("/campaigns", async (CreateCampaignRequest request, CreateCampaignUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(request, cancellationToken);
    return result.Success
        ? Results.Created($"/campaigns/{result.Content}", new { id = result.Content })
        : Results.BadRequest(new { error = result.ErrorMessage });
});

app.MapPost("/campaigns/{id:guid}/publish", async (Guid id, PublishCampaignUseCase useCase, CancellationToken cancellationToken) =>
{
    var result = await useCase.ExecuteAsync(id, cancellationToken);
    return result.Success
        ? Results.Ok()
        : Results.BadRequest(new { error = result.ErrorMessage });
});

app.Run();
