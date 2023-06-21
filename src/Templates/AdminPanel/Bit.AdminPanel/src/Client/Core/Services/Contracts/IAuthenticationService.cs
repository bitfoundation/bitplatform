using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.Client.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
