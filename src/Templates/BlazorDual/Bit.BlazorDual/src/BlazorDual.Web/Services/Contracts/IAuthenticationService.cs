using BlazorDual.Shared.Dtos.Identity;

namespace BlazorDual.Web.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
