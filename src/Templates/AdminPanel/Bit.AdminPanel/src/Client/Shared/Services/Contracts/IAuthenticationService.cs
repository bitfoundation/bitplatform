using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Shared.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
