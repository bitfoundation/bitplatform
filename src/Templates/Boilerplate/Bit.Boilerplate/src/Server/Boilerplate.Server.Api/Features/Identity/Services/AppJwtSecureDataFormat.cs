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
    private readonly ILogger<AppJwtSecureDataFormat> logger;
    private readonly TokenValidationParameters validationParameters;

    public AppJwtSecureDataFormat(ServerApiSettings appSettings,
        IHostEnvironment env,
        IConfiguration configuration,
        ILogger<AppJwtSecureDataFormat> logger,
        TimeProvider timeProvider,
        string tokenType)
    {
        this.logger = logger;
        this.tokenType = tokenType;
        this.appSettings = appSettings;
        this.timeProvider = timeProvider;

        // The two token classes are otherwise indistinguishable - same key, same issuer, same claim shape - so each
        // gets its own audience and validates only its own. Without that, a refresh token would authenticate ordinary
        // api calls for its full 14 day lifetime, and an access token replayed at Refresh would mint a new session.
        audience = tokenType is "AccessToken" ? appSettings.Identity.Audience : $"{appSettings.Identity.Audience}:{tokenType}";

        privateKey = AppCertificateService.GetPrivateSecurityKey(configuration);

        validationParameters = new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            IssuerSigningKeys = AppCertificateService.GetPublicSecurityKeys(configuration),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateIssuerSigningKey = env.IsDevelopment() is false,

            RequireExpirationTime = true,
            ValidateLifetime = tokenType is "AccessToken", /* IdentityController.Refresh will validate expiry itself while refreshing the token */

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuer = true,
            ValidIssuer = appSettings.Identity.Issuer,

            AuthenticationType = IdentityConstants.BearerScheme
        };
    }

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

            if (principal.IsInRole(AppRoles.GlobalAdmin))
            {
                foreach (var feat in AppFeatures.GetGlobalAdminFeatures())
                {
                    identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                }
            }
            //#if (multitenant == true)
            else if (principal.IsInRole(AppRoles.TenantAdmin))
            {
                foreach (var feat in AppFeatures.GetTenantAdminFeatures())
                {
                    identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                }
            }
            //#endif

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
                Issuer = appSettings.Identity.Issuer,
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
