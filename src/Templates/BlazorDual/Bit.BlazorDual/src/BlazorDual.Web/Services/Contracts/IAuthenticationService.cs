using BlazorDual.Shared.Dtos.Account;

namespace BlazorDual.Web.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
