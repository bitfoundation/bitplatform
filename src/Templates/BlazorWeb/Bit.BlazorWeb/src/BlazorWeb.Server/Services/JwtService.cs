﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using BlazorWeb.Server.Models.Identity;
using BlazorWeb.Shared.Dtos.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BlazorWeb.Server.Services;

public partial class JwtService : IJwtService
{
    [AutoInject] private readonly SignInManager<User> _signInManager = default!;

    [AutoInject] private readonly AppSettings _appSettings = default!;

    public async Task<SignInResponseDto> GenerateToken(User user)
    {
        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        RSA? rsaPrivateKey;
        using (X509Certificate2 signingCert = new X509Certificate2(certificatePath, _appSettings.JwtSettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet))
        {
            rsaPrivateKey = signingCert.GetRSAPrivateKey();
        }

        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsaPrivateKey), SecurityAlgorithms.RsaSha512);

        var claims = (await _signInManager.ClaimsFactory.CreateAsync(user)).Claims;

        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        var securityToken = jwtSecurityTokenHandler
            .CreateJwtSecurityToken(new SecurityTokenDescriptor
            {
                Issuer = _appSettings.JwtSettings.Issuer,
                Audience = _appSettings.JwtSettings.Audience,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow.AddMinutes(_appSettings.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.UtcNow.AddMinutes(_appSettings.JwtSettings.ExpirationMinutes),
                SigningCredentials = signingCredentials,
                Subject = new ClaimsIdentity(claims)
            });

        return new SignInResponseDto
        {
            AccessToken = jwtSecurityTokenHandler.WriteToken(securityToken),
            ExpiresIn = (long)TimeSpan.FromMinutes(_appSettings.JwtSettings.ExpirationMinutes).TotalSeconds
        };
    }
}
