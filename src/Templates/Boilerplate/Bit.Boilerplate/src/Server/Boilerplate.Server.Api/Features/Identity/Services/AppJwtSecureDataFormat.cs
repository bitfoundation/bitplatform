//+:cnd:noEmit
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// Stores bearer token in jwt format
/// </summary>
public partial class AppJwtSecureDataFormat
    : ISecureDataFormat<AuthenticationTicket>
{
    private readonly string tokenType;
    private readonly string audience;
    private readonly RsaSecurityKey privateKey;
    private readonly TimeProvider timeProvider;
    private readonly ServerApiSettings appSettings;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<AppJwtSecureDataFormat> logger;
    private readonly TokenValidationParameters validationParameters;

    public AppJwtSecureDataFormat(ServerApiSettings appSettings,
        IHostEnvironment env,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AppJwtSecureDataFormat> logger,
        TimeProvider timeProvider,
        string tokenType)
    {
        this.logger = logger;
        this.tokenType = tokenType;
        this.appSettings = appSettings;
        this.timeProvider = timeProvider;
        this.httpContextAccessor = httpContextAccessor;

        // The classes are otherwise indistinguishable - same key, issuer and claim shape - so each validates only its
        // own audience, or a refresh token would authenticate ordinary api calls and an access token replayed at
        // Refresh would mint a session. Anything else is an RFC 8707 resource identifier, and is its own audience.
        audience = tokenType switch
        {
            "AccessToken" => appSettings.Identity.Audience,
            "RefreshToken" => $"{appSettings.Identity.Audience}:{tokenType}",
            var resource => resource
        };

        // Refresh-class tokens are unprotected by code that checks expiry itself while rotating, so the protector must
        // not reject one first. Keyed off the type, or an OAuth access token - whose type is a url - would go unchecked.
        var isRefreshToken = tokenType.EndsWith("RefreshToken", StringComparison.Ordinal);

        privateKey = AppCertificateService.GetPrivateSecurityKey(configuration);

        validationParameters = CreateValidationParameters(configuration, env, appSettings, httpContextAccessor);
        validationParameters.ValidateLifetime = isRefreshToken is false;
        validationParameters.ValidateAudience = true;
        validationParameters.ValidAudience = audience;
        validationParameters.AuthenticationType = IdentityConstants.BearerScheme;
    }

    /// <summary>
    /// The rules every token this server signed is checked against, here and in <c>AppOAuthBearerOptionsConfigurator</c>,
    /// so key handling and trusted issuers cannot drift between the two schemes. Lifetime and audience are the caller's:
    /// they are what tell the token classes apart.
    /// </summary>
    public static TokenValidationParameters CreateValidationParameters(IConfiguration configuration,
        IHostEnvironment env,
        ServerApiSettings appSettings,
        IHttpContextAccessor httpContextAccessor)
    {
        return new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            IssuerSigningKeys = AppCertificateService.GetPublicSecurityKeys(configuration),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateIssuerSigningKey = env.IsDevelopment() is false,

            RequireExpirationTime = true,

            ValidateIssuer = true,
            IssuerValidator = (issuer, _, _) => appSettings.IsTrustedIssuer(issuer, httpContextAccessor.HttpContext?.Request)
                ? issuer
                : throw new SecurityTokenInvalidIssuerException($"'{issuer}' is not an origin this server issues tokens for.")
        };
    }

    /// <summary>
    /// RFC 8414 wants the issuer to be the url its metadata is served from, and this app only learns that from the
    /// request - localhost, a dev tunnel and production all differ. Keeps <c>iss</c>, the discovery document and the
    /// address the caller used in agreement, unconfigured.
    /// </summary>
    private string Issuer => httpContextAccessor.HttpContext?.Request.GetIssuer()
        ?? throw new InvalidOperationException("A token cannot be minted outside of a request.");

    public AuthenticationTicket? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(protectedText))
            {
                return Anonymous();
            }

            var handler = new JwtSecurityTokenHandler();

            // The default inbound map rewrites amr to a schemas.microsoft.com uri, so the claim written as amr comes
            // back under another name and AuthPolicies.TFA_ENABLED never matches it. The map is per instance, so
            // dropping the entry here leaves every other claim's mapping alone.
            handler.InboundClaimTypeMap.Remove(AppClaimTypes.AMR);

            var principal = handler.ValidateToken(protectedText, validationParameters, out var validToken);

            var validJwt = (JwtSecurityToken)validToken;
            var properties = new AuthenticationProperties() { ExpiresUtc = validJwt.ValidTo };

            var identity = new ClaimsIdentity(principal.Identity, null, IdentityConstants.BearerScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);

            foreach (var feat in AppFeatures.GetRoleImpliedFeatures(principal.IsInRole))
            {
                identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
            }

            var result = new ClaimsPrincipal(identity);

            var data = new AuthenticationTicket(result, properties: properties, IdentityConstants.BearerScheme);

            return data;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unprotect the {TokenType}.", tokenType);

            return Anonymous();
        }
    }

    private static AuthenticationTicket Anonymous()
    {
        return new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity()), string.Empty);
    }

    public string Protect(AuthenticationTicket data) => Protect(data, null);

    public string Protect(AuthenticationTicket data, string? purpose)
    {
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = audience,
                IssuedAt = timeProvider.GetUtcNow().UtcDateTime,
                Expires = data.Properties.ExpiresUtc!.Value.UtcDateTime,
                SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256Signature),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
