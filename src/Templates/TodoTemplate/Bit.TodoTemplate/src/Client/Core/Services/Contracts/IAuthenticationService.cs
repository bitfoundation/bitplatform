using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
