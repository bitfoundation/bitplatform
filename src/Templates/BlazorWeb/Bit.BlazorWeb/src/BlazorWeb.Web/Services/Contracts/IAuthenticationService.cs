using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Web.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
