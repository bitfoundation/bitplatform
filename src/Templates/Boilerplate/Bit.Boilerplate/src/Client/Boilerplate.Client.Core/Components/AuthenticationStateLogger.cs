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

            var (userId, userName, email, isUserAuthenticated) = user.IsAuthenticated() ? (user.GetUserId().ToString(), user.GetUserName(), user.GetEmail(), user.IsAuthenticated()) : default;

            LogAuthenticationState(authLogger, userId, userName, email, isUserAuthenticated);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Authentication State: {UserId}, {UserName}, {Email}, {IsUserAuthenticated}")]
    private static partial void LogAuthenticationState(ILogger logger, string userId, string userName, string? email, bool isUserAuthenticated);
}
