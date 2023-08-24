using AdminPanel.Shared.Dtos.Identity;

namespace AdminPanel.Client.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
