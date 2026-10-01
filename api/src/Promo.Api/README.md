# Promo.Api

The host — the only project in this solution that's a `Microsoft.NET.Sdk.Web` app. It wires `Promo.Api.Domain`, `Promo.Api.Application`, and `Promo.Api.Infrastructure` together into a running Minimal API, and owns everything specific to being an HTTP app: endpoint routing, Swagger, JWT auth, and global exception handling. See the [api root README](../../README.md) for the full Clean Architecture picture across all four projects.

```csharp
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
```

## Layout

```
Promo.Api/
├── Endpoints/            # CampaignEndpoints, AuthEndpoints — extension methods mapping routes
├── Configuration/         # SwaggerModule, JwtAuthenticationModule, ExceptionHandlingModule (all IStartup)
├── ExceptionHandling/      # GlobalExceptionHandler
├── Program.cs
└── appsettings.json
```

## Modules

`SwaggerModule` and `JwtAuthenticationModule` are `Components.Hosting.IStartup` too, same as `ApplicationModule`/`InfrastructureModule` — they only ever register services (`AddSwaggerGen`, `AddAuthentication().AddJwtBearer()`, `AddAuthorization()`). The middleware calls (`UseSwagger`, `UseAuthentication`, ...) stay in `Program.cs` itself, since `IStartup` only has a `ConfigureServices` hook, not a pipeline one — there's nowhere else for them to go.

> One naming collision worth knowing if you add another module here: the Web SDK's implicit usings bring in `Microsoft.AspNetCore.Hosting.IStartup` (ASP.NET Core's own legacy startup-class interface), which has the exact same name as `Components.Hosting.IStartup`. Every `Configuration/*.cs` file aliases it (`using IStartup = Components.Hosting.IStartup;`) to disambiguate — `Promo.Api.Application`/`Promo.Api.Infrastructure` never hit this because they're plain class libraries, not `Microsoft.NET.Sdk.Web`.

`GlobalExceptionHandler` (`Microsoft.AspNetCore.Diagnostics.IExceptionHandler`, .NET 8+) catches whatever an endpoint doesn't handle itself — a repository call failing because there's no database to reach, for instance — logs the real exception server-side, and returns a generic `ProblemDetails` (`{"title":"An unexpected error occurred.","status":500}`) instead of leaking a stack trace to the client. Business-rule failures never reach it: those already come back as `Result`, handled explicitly in each endpoint as a `400`. `ExceptionHandlingModule` registers it (`AddExceptionHandler<GlobalExceptionHandler>()` + `AddProblemDetails()`); `app.UseExceptionHandler()` is the first middleware in the pipeline, so it wraps everything after it.

## Endpoints

- `POST /auth/token` — body `{ "username": "..." }`. Returns a signed JWT. **Demo-only**: there's no real user store or password check, any non-empty username gets a token — this exists purely so `/campaigns` has something real to enforce against, not to model actual identity. Swap it for a real credential check before this protects anything that matters.
- `POST /campaigns` — requires `Authorization: Bearer <token>`. Body `{ "name": "...", "buyerGroupId": "..." }`. `201 Created` with the new id, `401` with no/invalid token, or `400` with the validation/not-found error.
- `POST /campaigns/{id}/publish` — requires a Bearer token. `200` on success, `401`/`400` as above.

Swagger UI (`/swagger`) has an **Authorize** button wired to the same Bearer scheme — paste a token from `/auth/token` there to call the protected endpoints interactively.

## Configuration

`appsettings.json`: `ConnectionStrings:SqlServer` and a `RabbitMq` section, same shape as `Components.SQLServerRepository`/`Components.Messaging.RabbitMQ`'s own samples, plus a `Jwt` section (`Issuer`, `Audience`, `SigningKey`) — the shipped `SigningKey` is a dev-only placeholder, replace it before this ever runs anywhere that matters. There's no `docker/` compose file yet (a separate, still-planned piece of this project), so `ConnectionStrings`/`RabbitMq` point at containers that don't exist until you stand them up yourself.

## Running it

```bash
cd api
dotnet build Promo.Api.slnx
dotnet run --project src/Promo.Api
```

Point `ConnectionStrings:SqlServer` at a real SQL Server, run `database/schema.sql` then `database/seed.sql` against it, and point `RabbitMq` at a real broker to exercise the full path. `database/seed.sql` uses fixed GUIDs on purpose:

- BuyerGroup `22222222-2222-2222-2222-222222222222` — VIP Customers
- BuyerGroup `33333333-3333-3333-3333-333333333333` — Newsletter Subscribers

```bash
TOKEN=$(curl -s -X POST http://localhost:<port>/auth/token \
  -H "Content-Type: application/json" -d '{"username":"julio"}' | jq -r .token)

curl -X POST http://localhost:<port>/campaigns \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d '{"name":"Black Friday","buyerGroupId":"22222222-2222-2222-2222-222222222222"}'
```

### Verified by actually running it

- `POST /campaigns` with no `Authorization` header → `401`, before touching validation or the database.
- `POST /campaigns` with an empty `name` (and a valid token) → `400`, with FluentValidation's own localized message (`'Name' deve ser informado.` in a pt-BR environment) — still never touches the database, proving validation happens before any repository call.
- `POST /campaigns` with a valid token and a valid body → passes both `AuthenticationMiddleware` and `AuthorizationMiddleware`, reaches a real SQL Server round trip through Dapper, and fails only at login (no matching `docker/` container yet) — confirming the whole chain (routing → auth → DI → use case → repository → Dapper → `Microsoft.Data.SqlClient`) is wired correctly end to end. The client sees `{"title":"An unexpected error occurred.","status":500}`; the real `SqlException` (login failed for user 'sa') is what actually lands in the server log.
- `GET /swagger/v1/swagger.json` → `200`.

No dedicated test project here — there's no logic to isolate (routing/middleware wiring is exactly what the manual verification above exercises), the same reasoning `Promo.Api.Infrastructure`'s repositories use.
