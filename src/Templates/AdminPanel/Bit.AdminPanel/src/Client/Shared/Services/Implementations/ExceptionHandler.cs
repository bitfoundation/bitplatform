//-:cnd:noEmit
using System.Diagnostics;

namespace AdminPanel.Client.Shared.Services.Implementations;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] IStringLocalizer<AppStrings> _localizer = default!;
    [AutoInject] IAuthenticationService _authenticationService = default!;

    async void SignOut()
    {
        try
        {
            await _authenticationService.SignOut();
        }
        catch (Exception exp)
        {
            Handle(exp);
        }
    }

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        if (exception is UnauthorizedException)
        {
            SignOut();

            return;
        }

#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = MessageBox.Show(exceptionMessage, _localizer[nameof(AppStrings.Error)]);
        Console.WriteLine(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            MessageBox.Show(knownException.Message, _localizer[nameof(AppStrings.Error)]);
        }
        else
        {
            MessageBox.Show(_localizer[nameof(AppStrings.UnknownException)], _localizer[nameof(AppStrings.Error)]);
        }
#endif

    }
}
