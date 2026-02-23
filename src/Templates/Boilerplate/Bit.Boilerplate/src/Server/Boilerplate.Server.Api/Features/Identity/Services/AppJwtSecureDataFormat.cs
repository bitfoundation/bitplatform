using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Server.Api.Features.Identity.Services;

/// <summary>
/// Stores bearer token in jwt format
/// </summary>
public partial class AppJwtSecureDataFormat
    : ISecureDataFormat<AuthenticationTicket>
{
    private readonly string tokenType;
    private readonly RsaSecurityKey privateKey;
    private readonly ServerApiSettings appSettings;
    private readonly ILogger<AppJwtSecureDataFormat> logger;
    private readonly TokenValidationParameters validationParameters;

    public AppJwtSecureDataFormat(ServerApiSettings appSettings,
        IHostEnvironment env,
        ILogger<AppJwtSecureDataFormat> logger,
        string tokenType)
    {
        this.logger = logger;
        this.tokenType = tokenType;
        this.appSettings = appSettings;

        privateKey = AppCertificateService.GetPrivateSecurityKey();

        validationParameters = new()
        {
            ClockSkew = TimeSpan.Zero,
            RequireSignedTokens = true,

            IssuerSigningKey = AppCertificateService.GetPublicSecurityKey(),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateIssuerSigningKey = env.IsDevelopment() is false,

            RequireExpirationTime = true,
            ValidateLifetime = tokenType is "AccessToken", /* IdentityController.Refresh will validate expiry itself while refreshing the token */

            ValidateAudience = true,
            ValidAudience = appSettings.Identity.Audience,

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
            if (string.IsNullOrEmpty(protectedText))
            {
                return Anonymous();
            }

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(protectedText, validationParameters, out var validToken);

            var validJwt = (JwtSecurityToken)validToken;
            var properties = new AuthenticationProperties() { ExpiresUtc = validJwt.ValidTo };

            var identity = new ClaimsIdentity(principal.Identity, principal.Claims, IdentityConstants.BearerScheme, ClaimTypes.NameIdentifier, ClaimTypes.Role);

            if (principal.IsInRole(AppRoles.SuperAdmin))
            {
                foreach (var feat in AppFeatures.GetSuperAdminFeatures())
                {
                    identity.AddClaim(new Claim(AppClaimTypes.FEATURES, feat.Value));
                }
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
                Issuer = appSettings.Identity.Issuer,
                Audience = appSettings.Identity.Audience,
                IssuedAt = DateTimeOffset.UtcNow.DateTime,
                Expires = data.Properties.ExpiresUtc!.Value.UtcDateTime,
                SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256Signature),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
