using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TodoTemplate.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Services.Implementations;

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
        var secretKey = Encoding.UTF8.GetBytes(_appSettings.JwtSettings.SecretKey);
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

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
