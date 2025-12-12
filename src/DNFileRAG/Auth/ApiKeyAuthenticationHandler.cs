using System.Security.Claims;
using System.Text.Encodings.Web;
using DNFileRAG.Core.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace DNFileRAG.Auth;

/// <summary>
/// Authentication handler for simple API key auth using the X-API-Key header.
/// When ApiSecurity:RequireApiKey is false (dev), this authenticates a default principal
/// so Role-based [Authorize] endpoints continue to work.
/// </summary>
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-API-Key";

    private readonly IOptions<ApiSecurityOptions> _securityOptions;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiSecurityOptions> securityOptions)
        : base(options, logger, encoder)
    {
        _securityOptions = securityOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var security = _securityOptions.Value;

        // Dev mode: allow requests to flow through authorized endpoints without an API key.
        if (!security.RequireApiKey)
        {
            var devClaims = new[]
            {
                new Claim(ClaimTypes.Name, "dev"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(ClaimTypes.Role, "reader")
            };
            var devIdentity = new ClaimsIdentity(devClaims, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(devIdentity), SchemeName)));
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var apiKey = apiKeyValues.ToString();
        var match = security.ApiKeys.FirstOrDefault(k => string.Equals(k.Key, apiKey, StringComparison.Ordinal));
        if (match == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(match.Name) ? "api-key" : match.Name),
            new(ClaimTypes.Role, string.IsNullOrWhiteSpace(match.Role) ? "reader" : match.Role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!Response.HasStarted)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "text/plain; charset=utf-8";
        }

        return Response.WriteAsync("Unauthorized");
    }
}


