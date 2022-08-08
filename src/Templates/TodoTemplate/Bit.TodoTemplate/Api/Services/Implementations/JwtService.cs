using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Services.Implementations;

public partial class JwtService : IJwtService
{
    [AutoInject] readonly SignInManager<User> signInManager = default!;

    [AutoInject] readonly IOptionsSnapshot<AppSettings> appSettings = default!;

    public async Task<SignInResponseDto> GenerateToken(User user)
    {
        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        RSA? rsaPrivateKey;
        using (X509Certificate2 signingCert = new X509Certificate2(certificatePath, appSettings.Value.JwtSettings.IdentityCertificatePassword))
        {
            rsaPrivateKey = signingCert.GetRSAPrivateKey();
        }

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsaPrivateKey), SecurityAlgorithms.RsaSha512);

        var claims = (await signInManager.ClaimsFactory.CreateAsync(user)).Claims;

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = appSettings.Value.JwtSettings.Issuer,
                Audience = appSettings.Value.JwtSettings.Audience,
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now.AddMinutes(appSettings.Value.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.Now.AddMinutes(appSettings.Value.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims)
            });

        return new SignInResponseDto
        {
            AccessToken = jwtSecurityTokenHandler.WriteToken(securityToken),
            ExpiresIn = (long)TimeSpan.FromMinutes(appSettings.Value.JwtSettings.ExpirationMinutes).TotalSeconds
        };
    }
}
