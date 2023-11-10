using WebTemplate.Shared.Dtos.Identity;

namespace WebTemplate.Web.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
