using AdminPanel.Server.Api.Models.Identity;
using AdminPanel.Shared.Dtos.Identity;

namespace AdminPanel.Server.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
