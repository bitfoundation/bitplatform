using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Services.Identity;

/// <summary>
/// Stores bearer token in jwt format
/// </summary>
public partial class AppJwtSecureDataFormat(ServerApiSettings appSettings, TokenValidationParameters validationParameters)
    : ISecureDataFormat<AuthenticationTicket>
{
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
                foreach (var per in AppFeatures.GetSuperAdminFeatures())
                {
                    identity.AddClaim(new Claim(AppClaimTypes.FEATURES, per.Value));
                }
            }

            var result = new ClaimsPrincipal(identity);

            var data = new AuthenticationTicket(result, properties: properties, IdentityConstants.BearerScheme);

            return data;
        }
        catch (Exception ex)
        {
            if (AppEnvironment.IsDev())
            {
                Console.WriteLine(ex); // since we do not have access to any logger at this point!
            }

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
                SigningCredentials = new SigningCredentials(validationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha512),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
