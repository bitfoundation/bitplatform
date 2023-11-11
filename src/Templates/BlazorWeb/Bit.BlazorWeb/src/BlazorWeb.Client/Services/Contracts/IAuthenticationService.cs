using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
