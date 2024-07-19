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

            var (isUserAuthenticated, userId, userName, email, sessionId) = user.IsAuthenticated() ? (user.IsAuthenticated(), user.GetUserId().ToString(), user.GetUserName(), user.GetEmail(), user.GetSessionId()) : default;

            LogAuthenticationState(authLogger, isUserAuthenticated, userId, userName, email, sessionId);
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Authentication State: {IsUserAuthenticated}, {UserId}, {UserName}, {Email}, {SessionId}")]
    private static partial void LogAuthenticationState(ILogger logger, bool isUserAuthenticated, string userId, string userName, string? email, string? sessionId);
}
