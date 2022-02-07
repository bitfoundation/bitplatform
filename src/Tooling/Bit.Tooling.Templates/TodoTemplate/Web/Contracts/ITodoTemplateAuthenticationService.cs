using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Contracts
{
    public interface ITodoTemplateAuthenticationService
    {
        Task SignIn(RequestTokenDto dto);

        Task SignOut();
    }
}
