using AdminPanelTemplate.Shared.Dtos.Account;

namespace AdminPanelTemplate.App.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
