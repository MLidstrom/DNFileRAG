// NOTE: Left in place for compatibility/backward reference, but DNFileRAG now uses a proper
// ASP.NET Core authentication scheme (see DNFileRAG.Auth.ApiKeyAuthenticationHandler).
// This middleware is intentionally not used in the request pipeline.
using System.Security.Claims;
using DNFileRAG.Core.Configuration;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Middleware;

public class ApiKeyMiddleware
{
    public const string HeaderName = "X-API-Key";

    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<ApiSecurityOptions> options)
    {
        var security = options.Value;

        if (!security.RequireApiKey)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var apiKeyValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing API key");
            return;
        }

        var apiKey = apiKeyValues.ToString();
        var match = security.ApiKeys.FirstOrDefault(k => string.Equals(k.Key, apiKey, StringComparison.Ordinal));
        if (match == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(match.Name) ? "api-key" : match.Name),
            new(ClaimTypes.Role, string.IsNullOrWhiteSpace(match.Role) ? "reader" : match.Role)
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "ApiKey"));

        await _next(context);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder app)
        => app.UseMiddleware<ApiKeyMiddleware>();
}


