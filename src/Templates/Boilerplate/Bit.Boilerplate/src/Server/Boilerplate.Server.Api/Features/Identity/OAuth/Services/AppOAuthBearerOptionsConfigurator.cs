//+:cnd:noEmit
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// Validation for <see cref="AppAuthSchemes.OAUTH_BEARER"/>, the scheme accepting tokens issued to external apps. A
/// configurator because the rules depend on the request: which origin the caller reached, and so which resources exist.
/// </summary>
public class AppOAuthBearerOptionsConfigurator(ServerApiSettings appSettings,
    IHostEnvironment env,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor) : IPostConfigureOptions<JwtBearerOptions>
{
    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        if (name is not AppAuthSchemes.OAUTH_BEARER)
            return;

        options.MapInboundClaims = false; // Keep amr and the feature claims under the names they were written with.

        // Same keys, algorithm and trusted issuers as the app's own tokens; only what tells this class apart is set here.
        var validationParameters = AppJwtSecureDataFormat.CreateValidationParameters(configuration, env, appSettings, httpContextAccessor);

        validationParameters.ValidateLifetime = true;

        // The audience must name the resource being requested, not merely one this deployment serves (CoversPath).
        // It is also what keeps the app's own access tokens out: theirs is a bare name.
        validationParameters.ValidateAudience = true;
        validationParameters.AudienceValidator = (audiences, _, _) =>
        {
            var issuer = Issuer;
            var requestPath = httpContextAccessor.HttpContext?.Request.Path.Value ?? "";

            return audiences.Any(audience => OAuthResources.TryNormalize(issuer, audience, out var canonical)
                                             && OAuthResources.CoversPath(canonical, issuer, requestPath));
        };

        validationParameters.NameClaimType = ClaimTypes.NameIdentifier;
        validationParameters.RoleClaimType = ClaimTypes.Role;

        options.TokenValidationParameters = validationParameters;

        options.Events = new()
        {
            OnChallenge = context =>
            {
                // RFC 9728 §5.1: without this pointer a client has nothing to discover. Named by the resource's
                // canonical path, not the request's, or a sub-path would point at metadata that 404s.
                var resourcePath = OAuthResources.Find(context.Request.Path.Value ?? "")?.Path ?? context.Request.Path.Value ?? "";

                context.Response.Headers.WWWAuthenticate = OAuthProtectedResource.ChallengeHeaderValue(Issuer, resourcePath);
                return Task.CompletedTask;
            }
        };
    }

    private string Issuer => httpContextAccessor.HttpContext?.Request.GetIssuer() ?? "";
}
