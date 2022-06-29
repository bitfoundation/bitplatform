using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Services.Contracts;

public interface ITodoTemplateAuthenticationService
{
    Task SignIn(SignInRequestDto dto);

    Task SignOut();
}
