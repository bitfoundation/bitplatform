using BlazorWeb.Shared.Dtos.Account;

namespace BlazorWeb.Web.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
