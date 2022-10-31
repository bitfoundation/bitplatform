using TodoTemplate.Server.Api.Models.Account;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Server.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
