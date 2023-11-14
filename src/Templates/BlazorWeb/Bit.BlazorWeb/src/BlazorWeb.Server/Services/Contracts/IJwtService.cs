using BlazorWeb.Server.Models.Identity;
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Server.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
