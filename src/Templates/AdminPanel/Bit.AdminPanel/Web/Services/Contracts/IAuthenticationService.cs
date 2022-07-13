using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
