using AdminPanel.Api.Models.Account;
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
