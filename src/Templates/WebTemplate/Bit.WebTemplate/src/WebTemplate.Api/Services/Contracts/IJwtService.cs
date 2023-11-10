using WebTemplate.Api.Models.Identity;
using WebTemplate.Shared.Dtos.Identity;

namespace WebTemplate.Api.Services.Contracts;

public interface IJwtService
{
    Task<SignInResponseDto> GenerateToken(User user);
}
