using TodoTemplate.Shared.Dtos.Identity;

namespace TodoTemplate.Client.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
