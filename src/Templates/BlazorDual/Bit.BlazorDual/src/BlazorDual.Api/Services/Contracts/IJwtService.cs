using BlazorDual.Api.Models.Account;
using BlazorDual.Shared.Dtos.Account;

namespace BlazorDual.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
