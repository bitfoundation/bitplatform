//+:cnd:noEmit
using Microsoft.AspNetCore.RateLimiting;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Shared.Features.Identity.OAuth.Dtos;
using Boilerplate.Server.Api.Features.Identity.OAuth.Services;

namespace Boilerplate.Server.Api.Features.Identity.OAuth;

/// <summary>
/// The endpoints an OAuth client itself talks to, plus the metadata that lets it find them. Minimal apis rather than
/// controller actions because the token endpoint is a form POST with no <c>Authorization</c> header - exactly what
/// <c>AutoCsrfProtectionFilter</c> rejects - and because clients discover these rather than target a version.
/// </summary>
public static class OAuthEndpoints
{
    public static WebApplication MapOAuthEndpoints(this WebApplication app)
    {
        // Rate limited like the other two: an unknown client id is a url this server goes and fetches.
        app.MapGet("/oauth/authorize", Authorize)
           .AllowAnonymous()
           .RequireRateLimiting(RateLimitOptionsExtensions.IDENTITY)
           .ExcludeFromDescription();

        app.MapPost("/oauth/token", Token)
           .AllowAnonymous()
           .DisableAntiforgery()
           .RequireRateLimiting(RateLimitOptionsExtensions.IDENTITY)
           .ExcludeFromDescription();

        app.MapGet("/.well-known/oauth-authorization-server", AuthorizationServerMetadata).AllowAnonymous();

        // RFC 9728 puts the well-known segment before the resource's path; the bare form is served because some clients
        // ask for it first.
        app.MapGet($"{OAuthProtectedResource.WellKnownPath}/{{**resourcePath}}", ProtectedResourceMetadata).AllowAnonymous();
        app.MapGet(OAuthProtectedResource.WellKnownPath, (HttpRequest request)
            => ProtectedResourceMetadata(request, OAuthResources.DevMcpPath.TrimStart('/'))).AllowAnonymous();

        return app;
    }

    /// <summary>
    /// What an MCP endpoint requires, read off the resource table. The OAuth scheme has to be named or every delegated
    /// token arrives anonymous; the requirements stay the registered policies, so tightening one applies here too.
    /// </summary>
    public static IAuthorizeData[] AuthorizationFor(string resourcePath)
    {
        var schemes = $"{IdentityConstants.BearerScheme},{AppAuthSchemes.OAUTH_BEARER}";

        return [new AuthorizeAttribute { AuthenticationSchemes = schemes },
                .. OAuthResources.PoliciesFor(resourcePath).Select(policy => new AuthorizeAttribute(policy))];
    }

    /// <summary>Tells a client which authorization server guards this resource, and what to ask it for.</summary>
    private static IResult ProtectedResourceMetadata(HttpRequest request, string? resourcePath)
    {
        var issuer = request.GetIssuer();

        var path = $"/{(resourcePath ?? "").Trim('/')}";

        if (OAuthResources.TryNormalize(issuer, $"{issuer}{path}", out _) is false)
            return Results.NotFound();

        return Results.Json(OAuthProtectedResource.Metadata(issuer, path));
    }

    /// <summary>
    /// Validates the request and hands the browser to the consent page; it renders nothing itself, because the ui lives
    /// in the Blazor app, where the user is (or is about to be) signed in.
    /// </summary>
    private static async Task<IResult> Authorize(HttpRequest httpRequest, OAuthService oauthService, CancellationToken cancellationToken)
    {
        var request = ParseAuthorizeRequest(httpRequest);

        var validated = await oauthService.Validate(request, cancellationToken);

        // The uri has not been proven to belong to anybody, so honouring it even to report an error is an open redirect.
        if (validated.CanRedirect is false)
            return Results.Redirect(ConsentPageUrl(httpRequest, $"?error={Uri.EscapeDataString(validated.Error!)}"));

        if (validated.IsValid is false)
            return Results.Redirect(oauthService.BuildErrorRedirectUrl(request.RedirectUri!, validated.Error, request.State));

        // Travels on unchanged; the consent page hands it back and the server revalidates from scratch.
        return Results.Redirect(ConsentPageUrl(httpRequest, httpRequest.QueryString.Value));
    }

    private static async Task<IResult> Token(HttpRequest httpRequest, OAuthTokenService tokenService, CancellationToken cancellationToken)
    {
        // RFC 6749 §5.1: a token response must never be cached, by the client or by anything between.
        httpRequest.HttpContext.Response.Headers.CacheControl = "no-store";
        httpRequest.HttpContext.Response.Headers.Pragma = "no-cache";

        if (httpRequest.HasFormContentType is false)
            return OAuthError("invalid_request");

        try
        {
            var response = await tokenService.Exchange(await httpRequest.ReadFormAsync(cancellationToken), cancellationToken);

            return Results.Json(new
            {
                access_token = response.AccessToken,
                token_type = "Bearer",
                expires_in = response.ExpiresIn,
                refresh_token = response.RefreshToken,
                scope = response.Scope
            });
        }
        catch (BadRequestException exception)
        {
            // RFC 6749's error body: clients parse this, and problem-details would be unreadable to them.
            return OAuthError(exception.Data["Reason"]?.ToString() ?? "invalid_request");
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or ConflictException)
        {
            // The second of two exchanges racing over one code: the code's concurrency token failed its save.
            return OAuthError("invalid_grant");
        }
    }

    private static IResult OAuthError(string error) => Results.Json(new { error }, statusCode: 400);

    /// <summary>
    /// RFC 8414. Everything is derived from the request, so one deployment answers correctly on localhost, on a dev
    /// tunnel and on its own domain without being told which it is.
    /// </summary>
    private static IResult AuthorizationServerMetadata(HttpRequest httpRequest)
    {
        var issuer = httpRequest.GetIssuer();

        return Results.Json(new
        {
            issuer,
            authorization_endpoint = $"{issuer}/oauth/authorize",
            token_endpoint = $"{issuer}/oauth/token",
            jwks_uri = $"{issuer}/.well-known/jwks",
            response_types_supported = new[] { "code" },
            grant_types_supported = new[] { "authorization_code", "refresh_token" },
            code_challenge_methods_supported = new[] { "S256" },
            token_endpoint_auth_methods_supported = new[] { "none" },
            scopes_supported = OAuthScopes.All,
            // The only way a client with no prior relationship registers: its client_id is the https url of its own
            // metadata document. RFC 7591 is deprecated by MCP and deliberately not implemented.
            client_id_metadata_document_supported = true,
            // RFC 9207: the iss on the authorization response lets a client prove which server answered it.
            authorization_response_iss_parameter_supported = true
        });
    }

    /// <summary>
    /// The consent page lives in the Blazor app, which with a standalone api is another domain, and an OAuth client
    /// arrives with no <c>origin</c> or <c>X-Origin</c> to say so - hence <c>WebAppUrl</c> in configuration.
    /// Concatenated, not combined through <see cref="Uri"/>, whose <c>ToString()</c> unescapes the client's query:
    /// a non-ascii <c>state</c> then fails Kestrel's header validation.
    /// </summary>
    private static string ConsentPageUrl(HttpRequest httpRequest, string? query)
    {
        return $"{httpRequest.GetWebAppUrl().GetLeftPart(UriPartial.Authority)}{PageUrls.OAuthConsent}{query}";
    }

    /// <summary>Written out because the wire names are snake_case and model binding would silently bind none of them.</summary>
    private static OAuthAuthorizeRequestDto ParseAuthorizeRequest(HttpRequest request)
    {
        return new OAuthAuthorizeRequestDto
        {
            ClientId = request.Query["client_id"],
            RedirectUri = request.Query["redirect_uri"],
            ResponseType = request.Query["response_type"],
            Scope = request.Query["scope"],
            State = request.Query["state"],
            CodeChallenge = request.Query["code_challenge"],
            CodeChallengeMethod = request.Query["code_challenge_method"],
            Resource = request.Query["resource"],
            //#if (multitenant == true)
            TenantId = Guid.TryParse(request.Query["tenant_id"], out var tenantId) ? tenantId : null
            //#endif
        };
    }
}
