using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components;
public partial class AuthenticationStateLogger : AppComponentBase
{
    [AutoInject] private ILogger<AuthenticationStateLogger> authLogger = default!;

    protected async override Task OnInitAsync()
    {
        AuthenticationManager.AuthenticationStateChanged += LogAuthenticationState;

        LogAuthenticationState(AuthenticationManager.GetAuthenticationStateAsync());

        await base.OnInitAsync();
    }

    private async void LogAuthenticationState(Task<AuthenticationState> task)
    {
        try
        {
            var user = (await AuthenticationStateTask).User;

            var (userId, email, isUserAuthenticated) = user.IsAuthenticated() ? (user.GetUserId().ToString(), user.GetEmail(), user.IsAuthenticated()) : default;

            authLogger.LogInformation("Authentication State: {UserId}, {Email}, {IsUserAuthenticated}", userId, email, isUserAuthenticated);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }
}
