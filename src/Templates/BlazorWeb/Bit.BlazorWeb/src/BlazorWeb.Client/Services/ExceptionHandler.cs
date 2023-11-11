//-:cnd:noEmit
using System.Diagnostics;

namespace BlazorWeb.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] IStringLocalizer<AppStrings> _localizer = default!;
    [AutoInject] IAuthenticationService _authenticationService = default!;
    [AutoInject] MessageBoxService _messageBoxService = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        if (exception is UnauthorizedException)
        {
            SignOut();

            return;
        }

#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = _messageBoxService.Show(exceptionMessage, _localizer[nameof(AppStrings.Error)]);
        _ = Console.Out.WriteLineAsync(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = _messageBoxService.Show(knownException.Message, _localizer[nameof(AppStrings.Error)]);
        }
        else
        {
            _ = _messageBoxService.Show(_localizer[nameof(AppStrings.UnknownException)], _localizer[nameof(AppStrings.Error)]);
        }
#endif
    }

    private async void SignOut()
    {
        try
        {
            await _messageBoxService.Show(_localizer[nameof(AppStrings.YouNeedToSignIn)], _localizer[nameof(AppStrings.Error)]);

            await _authenticationService.SignOut();
        }
        catch (Exception exp)
        {
            Handle(exp);
        }
    }
}
