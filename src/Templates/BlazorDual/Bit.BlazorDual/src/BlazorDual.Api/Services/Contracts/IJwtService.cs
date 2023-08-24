using BlazorDual.Api.Models.Identity;
using BlazorDual.Shared.Dtos.Identity;

namespace BlazorDual.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
