using AdminPanel.Server.Api.Models.Account;
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Server.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
