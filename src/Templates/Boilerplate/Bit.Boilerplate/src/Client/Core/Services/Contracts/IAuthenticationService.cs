using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Services.Contracts;

public interface IAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
