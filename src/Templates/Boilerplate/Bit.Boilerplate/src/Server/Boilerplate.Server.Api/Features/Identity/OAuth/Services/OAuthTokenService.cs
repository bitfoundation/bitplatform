//+:cnd:noEmit
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Boilerplate.Shared.Features.Identity.OAuth;
using Boilerplate.Server.Api.Features.Identity.OAuth.Models;

namespace Boilerplate.Server.Api.Features.Identity.OAuth.Services;

/// <summary>
/// Mints the tokens an external app gets: this app's own RS256 JWTs, narrowed by an <c>aud</c> of the one requested
/// resource and only the features the user consented to delegate.
/// </summary>
public partial class OAuthTokenService(AppDbContext dbContext,
    UserManager<User> userManager,
    AppUserClaimsPrincipalFactory userClaimsPrincipalFactory,
    OAuthService oauthService,
    OAuthClientResolver clientResolver,
    ServerApiSettings appSettings,
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider,
    ILogger<OAuthTokenService> logger)
{
    /// <summary>Carried so a refresh detects a changed password or newly enabled 2FA, as <c>IdentityController.Refresh</c> does.</summary>
    private const string SecurityStampClaimType = "AspNet.Identity.SecurityStamp";

    private HttpContext HttpContext => httpContextAccessor.HttpContext!;

    public async Task<OAuthTokenResponse> Exchange(IFormCollection form, CancellationToken cancellationToken)
    {
        var grantType = form["grant_type"].ToString();

        return grantType switch
        {
            "authorization_code" => await ExchangeAuthorizationCode(form, cancellationToken),
            "refresh_token" => await ExchangeRefreshToken(form, cancellationToken),
            // implicit and password are gone from OAuth 2.1, and client_credentials has no user to consent.
            _ => throw new BadRequestException().WithData("Reason", "unsupported_grant_type")
        };
    }

    private async Task<OAuthTokenResponse> ExchangeAuthorizationCode(IFormCollection form, CancellationToken cancellationToken)
    {
        var clientId = form["client_id"].ToString();

        var stored = await oauthService.ConsumeCode(form["code"].ToString(),
                                                    clientId,
                                                    form["redirect_uri"].ToString(),
                                                    form["code_verifier"].ToString(),
                                                    cancellationToken);

        // RFC 8707: the token request may narrow the resource, never widen it. Normalised as the authorize request was,
        // or one spelling passes there and fails here.
        var requestedResource = form["resource"].ToString();
        if (string.IsNullOrWhiteSpace(requestedResource) is false
            && (OAuthResources.TryNormalize(oauthService.Issuer, requestedResource, out var canonicalResource) is false
                || string.Equals(canonicalResource, stored.Resource, StringComparison.Ordinal) is false))
        {
            throw new BadRequestException().WithData("Reason", "invalid_target");
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString())
                   ?? throw new BadRequestException().WithData("Reason", "invalid_grant");

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        var client = await clientResolver.Resolve(stored.ClientId, cancellationToken);

        // A grant is a UserSession like any other sign-in, so Settings -> Sessions lists it and its revoke button ends
        // it. The refresh id is decided here so the consumed code, the session and the valid token land in one save.
        var refreshTokenId = Guid.CreateVersion7();

        var userSession = new UserSession
        {
            Id = Guid.CreateSequentialGuid(),
            UserId = user.Id,
            StartedOn = timeProvider.GetUtcNow().ToUnixTimeSeconds(),
            IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
            //#if (cloudflare == true)
            // Same source CreateUserSession uses, so a grant reads like every other row in the sessions list.
            Address = $"{HttpContext.Request.Headers["cf-ipcountry"]}, {HttpContext.Request.Headers["cf-ipcity"]}",
            //#endif
            //#if (multitenant == true)
            TenantId = stored.TenantId,
            //#endif
            // DeviceInfo stays empty: this is not a device, and the application's name belongs on the grant.
            OAuthGrant = new OAuthGrant
            {
                ClientId = stored.ClientId,
                ClientName = client?.ClientName,
                Scope = stored.Scope,
                Resource = stored.Resource,
                RefreshTokenId = refreshTokenId
            }
        };

        await dbContext.UserSessions.AddAsync(userSession, cancellationToken);

        stored.UserSessionId = userSession.Id;

        // The code's concurrency token fails the loser of a race here (OAuthEndpoints.Token answers invalid_grant).
        await dbContext.SaveChangesAsync(cancellationToken);

        Guid? grantedTenantId = null;
        //#if (multitenant == true)
        grantedTenantId = stored.TenantId;
        //#endif

        return await Issue(user, userSession, stored.Resource, OAuthScopes.Parse(stored.Scope), stored.ClientId, grantedTenantId, stored.Amr, refreshTokenId);
    }

    private async Task<OAuthTokenResponse> ExchangeRefreshToken(IFormCollection form, CancellationToken cancellationToken)
    {
        var refreshToken = form["refresh_token"].ToString();

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BadRequestException().WithData("Reason", "invalid_request");

        // Read unvalidated only to pick the protector, which then validates everything. It has to be a resource of
        // ours first, or the app's own refresh token - audience a bare name - would pick a protector that accepts it.
        var claimedResource = ReadResourceClaim(refreshToken);

        if (OAuthResources.TryNormalize(oauthService.Issuer, claimedResource, out var resource) is false)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        var ticket = ProtectorFor($"{resource}:RefreshToken").Unprotect(refreshToken);

        if (ticket?.Principal?.IsAuthenticated() is not true)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        if ((ticket.Properties.ExpiresUtc ?? DateTimeOffset.MinValue) < timeProvider.GetUtcNow())
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        var clientId = ticket.Principal.GetClaimValue<string?>(AppClaimTypes.OAUTH_CLIENT_ID);

        // The token is bound to the client it was issued to; presenting it as another client is theft, not a mix-up.
        if (string.IsNullOrEmpty(clientId) || string.Equals(clientId, form["client_id"].ToString(), StringComparison.Ordinal) is false)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        var sessionId = ticket.Principal.GetSessionId();

        var userSession = await dbContext.UserSessions
            .Include(session => session.User)
            .Include(session => session.OAuthGrant)
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken)
            ?? throw new BadRequestException().WithData("Reason", "invalid_grant"); // Revoked from Settings -> Sessions.

        var user = userSession.User!;

        // A session with no grant is one of the app's own; its refresh token belongs to IdentityController.
        if (userSession.OAuthGrant is null)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        // Only the most recently issued id is accepted, so a replay is caught however closely it follows; OAuth 2.1
        // §4.3.1 then requires the grant itself to go.
        var presentedTokenId = ticket.Principal.GetClaimValue<string?>(JwtRegisteredClaimNames.Jti);

        if (Guid.TryParse(presentedTokenId, out var tokenId) is false || tokenId != userSession.OAuthGrant.RefreshTokenId)
        {
            logger.LogWarning("Refresh token reuse detected for OAuth client {ClientId}. Revoking session {SessionId}.", clientId, sessionId);
            dbContext.UserSessions.Remove(userSession);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new BadRequestException().WithData("Reason", "invalid_grant");
        }

        var securityStamp = ticket.Principal.GetClaimValue<string?>(SecurityStampClaimType);
        if (string.Equals(await userManager.GetSecurityStampAsync(user), securityStamp, StringComparison.Ordinal) is false)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        if (await userManager.IsLockedOutAsync(user))
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        var refreshTokenId = Guid.CreateVersion7();
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        // One conditional update, not read-check-write: two refreshes racing would both pass the check above, and the
        // client's next refresh would then look like the replay.
        var rotated = await dbContext.OAuthGrants
            .Where(grant => grant.UserSessionId == sessionId && grant.RefreshTokenId == tokenId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(grant => grant.RefreshTokenId, refreshTokenId), cancellationToken);

        if (rotated is 0)
            throw new BadRequestException().WithData("Reason", "invalid_grant");

        // Separate statement: RenewedOn is the session's, and it is what the retention sweep goes on.
        await dbContext.UserSessions
            .Where(session => session.Id == sessionId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.RenewedOn, now), cancellationToken);

        // Re-read from the token rather than from the request: a refresh must never widen what was consented to.
        var scopes = OAuthScopes.Parse(ticket.Principal.GetClaimValue<string?>("scope"));

        var amr = string.Join(' ', ticket.Principal.FindAll(AppClaimTypes.AMR).Select(claim => claim.Value));

        Guid? sessionTenantId = null;
        //#if (multitenant == true)
        sessionTenantId = userSession.TenantId;
        //#endif

        return await Issue(user, userSession, resource, scopes, clientId, sessionTenantId, amr, refreshTokenId);
    }

    /// <summary>
    /// Builds the narrowed principal and mints the pair. Writes nothing: the caller has already recorded
    /// <paramref name="refreshTokenId"/> on the session.
    /// </summary>
    private async Task<OAuthTokenResponse> Issue(User user,
        UserSession userSession,
        string resource,
        string[] scopes,
        string clientId,
        Guid? tenantId,
        string? amr,
        Guid refreshTokenId)
    {
        var identity = new ClaimsIdentity(IdentityConstants.BearerScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(SecurityStampClaimType, await userManager.GetSecurityStampAsync(user)));
        identity.AddClaim(new Claim(AppClaimTypes.SESSION_ID, userSession.Id.ToString()));
        identity.AddClaim(new Claim(AppClaimTypes.OAUTH_CLIENT_ID, clientId));
        identity.AddClaim(new Claim("scope", string.Join(' ', scopes)));

        // /dev-mcp asks for AuthPolicies.TFA_ENABLED on top of the feature, so the token has to say how the user
        // authenticated when they granted it.
        foreach (var method in (amr ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            identity.AddClaim(new Claim(AppClaimTypes.AMR, method));
        }

        //#if (multitenant == true)
        if (tenantId is not null)
        {
            identity.AddClaim(new Claim(AppClaimTypes.TENANT_ID, tenantId.Value.ToString()));
        }
        //#endif

        // No role claims, deliberately: RoleClaimType is set, so a role would make IsInRole true everywhere this token
        // reaches, and survive into the refresh token, where AppJwtSecureDataFormat expands GlobalAdmin into everything.
        var userFeatures = await CurrentFeaturesOf(user, tenantId);

        foreach (var feature in OAuthScopes.FeaturesFor(scopes).Where(userFeatures.Contains))
        {
            identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feature));
        }

        var now = timeProvider.GetUtcNow();
        var accessTokenExpiresOn = now.Add(appSettings.Identity.BearerTokenExpiration);

        var accessToken = ProtectorFor(resource)
            .Protect(new AuthenticationTicket(new ClaimsPrincipal(identity), new AuthenticationProperties { ExpiresUtc = accessTokenExpiresOn }, IdentityConstants.BearerScheme));

        // The id the session names as valid; rotation replaces it, which is what makes a replay detectable.
        var refreshIdentity = new ClaimsIdentity(identity.Claims, IdentityConstants.BearerScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);
        refreshIdentity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, refreshTokenId.ToString()));

        var refreshToken = ProtectorFor($"{resource}:RefreshToken")
            .Protect(new AuthenticationTicket(new ClaimsPrincipal(refreshIdentity), new AuthenticationProperties { ExpiresUtc = now.Add(appSettings.OAuth.RefreshTokenExpiration) }, IdentityConstants.BearerScheme));

        return new OAuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = (long)appSettings.Identity.BearerTokenExpiration.TotalSeconds,
            Scope = string.Join(' ', scopes)
        };
    }

    /// <summary>
    /// Re-read on every issue and refresh, so a feature removed in the admin panel is gone from the next token.
    /// </summary>
    private async Task<HashSet<string>> CurrentFeaturesOf(User user, Guid? tenantId)
    {
        //#if (multitenant == true)
        // The factory reads the tenant off the request and a token request is anonymous, so without this every
        // tenant-scoped role and claim is missed.
        if (tenantId is not null)
        {
            userClaimsPrincipalFactory.SetTenantId(tenantId.Value);
        }
        //#endif

        var principal = await userClaimsPrincipalFactory.CreateAsync(user);

        var features = principal.FindAll(AppClaimTypes.FEATURES).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);

        // The factory does not expand roles into features; every token reader does, or an admin could delegate nothing.
        features.UnionWith(AppFeatures.GetRoleImpliedFeatures(principal.IsInRole).Select(feature => feature.Value));

        return features;
    }

    private AppJwtSecureDataFormat ProtectorFor(string tokenType)
    {
        return ActivatorUtilities.CreateInstance<AppJwtSecureDataFormat>(serviceProvider, tokenType);
    }

    private static string? ReadResourceClaim(string refreshToken)
    {
        try
        {
            var audience = new JwtSecurityToken(refreshToken).Audiences.FirstOrDefault();

            return audience?.EndsWith(":RefreshToken", StringComparison.Ordinal) is true
                ? audience[..^":RefreshToken".Length]
                : null;
        }
        // The caller controls this string, and anything that is not a jwt throws SecurityTokenMalformedException -
        // a 500 rather than invalid_grant.
        catch (Exception exception) when (exception is ArgumentException or FormatException or SecurityTokenException)
        {
            return null;
        }
    }
}

/// <summary>An RFC 6749 token response; property names are the wire names.</summary>
public partial class OAuthTokenResponse
{
    public string AccessToken { get; set; } = default!;

    public string RefreshToken { get; set; } = default!;

    public long ExpiresIn { get; set; }

    public string Scope { get; set; } = default!;
}
