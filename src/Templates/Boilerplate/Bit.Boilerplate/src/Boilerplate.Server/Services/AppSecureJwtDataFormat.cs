using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace Boilerplate.Server.Services;

/// <summary>
/// Stores bearer token in jwt format
/// </summary>
public class AppSecureJwtDataFormat(AppSettings appSettings, TokenValidationParameters validationParameters)
    : ISecureDataFormat<AuthenticationTicket>
{
    public AuthenticationTicket? Unprotect(string? protectedText)
        => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        try
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return NotSignedIn();
            }

            var handler = new JwtSecurityTokenHandler();
            ClaimsPrincipal? principal = handler.ValidateToken(protectedText, validationParameters, out var validToken);
            var validJwt = (JwtSecurityToken)validToken;
            var data = new AuthenticationTicket(principal, properties: new AuthenticationProperties()
            {
                ExpiresUtc = validJwt.ValidTo
            }, IdentityConstants.BearerScheme);
            return data;
        }
        catch (Exception exp)
        {
            throw new UnauthorizedException(nameof(AppStrings.UnauthorizedException), exp);
        }
    }

    private AuthenticationTicket NotSignedIn()
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
                Issuer = appSettings.IdentitySettings.Issuer,
                Audience = appSettings.IdentitySettings.Audience,
                IssuedAt = DateTime.UtcNow,
                Expires = data.Properties.ExpiresUtc!.Value.UtcDateTime,
                SigningCredentials = new SigningCredentials(validationParameters.IssuerSigningKey, SecurityAlgorithms.RsaSha512),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
