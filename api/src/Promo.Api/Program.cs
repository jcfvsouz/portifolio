using Components.Hosting;
using Promo.Api.Application;
using Promo.Api.Configuration;
using Promo.Api.Endpoints;
using Promo.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.UseStartup<ApplicationModule>();
builder.UseStartup<InfrastructureModule>();
builder.UseStartup<JwtAuthenticationModule>();
builder.UseStartup<SwaggerModule>();
builder.UseStartup<ExceptionHandlingModule>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCampaignEndpoints();
app.Run();
