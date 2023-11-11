using BlazorWeb.Api.Models.Identity;
using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
