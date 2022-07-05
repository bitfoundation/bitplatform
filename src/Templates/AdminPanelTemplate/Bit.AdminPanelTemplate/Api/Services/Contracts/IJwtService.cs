using AdminPanelTemplate.Api.Models.Account;
using AdminPanelTemplate.Shared.Dtos.Account;

namespace AdminPanelTemplate.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
