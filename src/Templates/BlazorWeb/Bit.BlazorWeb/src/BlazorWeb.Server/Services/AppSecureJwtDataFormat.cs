using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace BlazorWeb.Server.Services;

public class AppSecureJwtDataFormat(AppSettings appSettings, TokenValidationParameters validationParameters)
    : ISecureDataFormat<AuthenticationTicket>
{
    private readonly TokenValidationParameters validationParameters = validationParameters;

    public AuthenticationTicket? Unprotect(string? protectedText)
        => Unprotect(protectedText, null);
    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        var handler = new JwtSecurityTokenHandler();
        ClaimsPrincipal? principal;
        try
        {
            principal = handler.ValidateToken(protectedText, validationParameters, out var validToken);
            if (validToken is not JwtSecurityToken validJwt)
            {
                throw new ArgumentException("Invalid JWT");
            }
            if (!validJwt.Header.Alg.Equals(SecurityAlgorithms.EcdsaSha256, StringComparison.Ordinal))
            {
                throw new ArgumentException($"Algorithm must be '{SecurityAlgorithms.RsaSha512}'");
            }
        }
        catch (SecurityTokenValidationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        return new AuthenticationTicket(principal, new AuthenticationProperties(), CookieAuthenticationDefaults.AuthenticationScheme);
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
                Expires = DateTime.UtcNow + appSettings.IdentitySettings.BearerTokenExpiration,
                SigningCredentials = new SigningCredentials(validationParameters.IssuerSigningKey, SecurityAlgorithms.RsaSha512),
                Subject = new ClaimsIdentity(data.Principal.Claims),
            });

        var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

        return encodedJwt;
    }
}
