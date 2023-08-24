using TodoTemplate.Server.Api.Models.Identity;
using TodoTemplate.Shared.Dtos.Identity;

namespace TodoTemplate.Server.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
