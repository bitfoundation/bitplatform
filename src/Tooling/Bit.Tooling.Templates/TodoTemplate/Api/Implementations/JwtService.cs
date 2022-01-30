using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoTemplate.Api.Contracts;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Implementations
{
    public class JwtService : IJwtService
    {
        private readonly UserManager<User> _userManager;

        private readonly AppSettings _appSettings;

        public JwtService(UserManager<User> userManager, IOptionsSnapshot<AppSettings> setting)
        {
            _userManager = userManager;
            _appSettings = setting.Value;
        }

        public async Task<ResponseTokenDto> GenerateToken(RequestTokenDto dto)
        {
            var secretKey = Encoding.UTF8.GetBytes(_appSettings.JwtSettings.SecretKey);
            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature);

            var encryptionKey = Encoding.UTF8.GetBytes(_appSettings.JwtSettings.EncryptKey);
            var encryptingCredentials = new EncryptingCredentials(new SymmetricSecurityKey(encryptionKey), SecurityAlgorithms.Aes128KW, SecurityAlgorithms.Aes128CbcHmacSha256);

            var user = await _userManager.FindByNameAsync(dto.UserName);
            var claims = await _userManager.GetClaimsAsync(user);

            var securityToken = new JwtSecurityTokenHandler()
                .CreateJwtSecurityToken(new SecurityTokenDescriptor
                {
                    Issuer = _appSettings.JwtSettings.Issuer,
                    Audience = _appSettings.JwtSettings.Audience,
                    IssuedAt = DateTime.Now,
                    NotBefore = DateTime.Now.AddMinutes(_appSettings.JwtSettings.NotBeforeMinutes),
                    Expires = DateTime.Now.AddMinutes(_appSettings.JwtSettings.ExpirationMinutes),
                    SigningCredentials = signingCredentials,
                    EncryptingCredentials = encryptingCredentials,
                    Subject = new ClaimsIdentity(claims)
                });

            return new ResponseTokenDto { Token = new JwtSecurityTokenHandler().WriteToken(securityToken) };
        }
    }
}
