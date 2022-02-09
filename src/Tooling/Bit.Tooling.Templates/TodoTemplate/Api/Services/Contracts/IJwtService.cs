using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(SignInRequestDto dto);
}
