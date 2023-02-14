using BlazorWeb.Api.Models.Account;
using BlazorWeb.Shared.Dtos.Account;

namespace BlazorWeb.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
