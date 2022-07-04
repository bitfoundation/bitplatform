using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
