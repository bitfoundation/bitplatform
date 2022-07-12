﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Tokens;
using AdminPanel.Api.Models.Account;
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Api.Services.Implementations;

public class JwtService : IJwtService
{
    private readonly SignInManager<User> _signInManager;

    private readonly AppSettings _appSettings;

    public JwtService(SignInManager<User> signInManager, IOptionsSnapshot<AppSettings> setting)
    {
        _signInManager = signInManager;
        _appSettings = setting.Value;
    }

    public async Task<SignInResponseDto> GenerateToken(User user)
    {
        var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
        RSA? rsaPrivateKey;
        using (X509Certificate2 signingCert = new X509Certificate2(certificatePath, _appSettings.JwtSettings.IdentityCertificatePassword))
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
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now.AddMinutes(_appSettings.JwtSettings.NotBeforeMinutes),
                Expires = DateTime.Now.AddMinutes(_appSettings.JwtSettings.ExpirationMinutes),
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
