using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
