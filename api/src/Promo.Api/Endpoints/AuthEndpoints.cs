using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Promo.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/token", IssueToken);
        return app;
    }

    private static IResult IssueToken(TokenRequest request, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
            return Results.BadRequest(new { error = "Username is required." });

        var jwt = configuration.GetSection("Jwt");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: [new Claim(ClaimTypes.Name, request.Username)],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public record TokenRequest(string Username);
