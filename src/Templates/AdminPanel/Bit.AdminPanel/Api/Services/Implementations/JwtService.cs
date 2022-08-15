using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using AdminPanel.Api.Models.Account;
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Api.Services.Implementations;

public partial class JwtService : IJwtService
{
    [AutoInject] private readonly SignInManager<User> _signInManager = default!;

    [AutoInject] private readonly IOptionsSnapshot<AppSettings> _appSettings = default!;

    public async Task<SignInResponseDto> GenerateToken(User user)
    {
        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        RSA? rsaPrivateKey;
        using (X509Certificate2 signingCert = new X509Certificate2(certificatePath, _appSettings.Value.JwtSettings.IdentityCertificatePassword))
        {
            rsaPrivateKey = signingCert.GetRSAPrivateKey();
        }

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsaPrivateKey), SecurityAlgorithms.RsaSha512);

        var claims = (await _signInManager.ClaimsFactory.CreateAsync(user)).Claims;

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.Value.JwtSettings.Issuer,
                Audience = _appSettings.Value.JwtSettings.Audience,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now.AddMinutes(_appSettings.Value.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.Now.AddMinutes(_appSettings.Value.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims)
            });

        return new SignInResponseDto
        {
            AccessToken = jwtSecurityTokenHandler.WriteToken(securityToken),
            ExpiresIn = (long)TimeSpan.FromMinutes(_appSettings.Value.JwtSettings.ExpirationMinutes).TotalSeconds
        };
    }
}
