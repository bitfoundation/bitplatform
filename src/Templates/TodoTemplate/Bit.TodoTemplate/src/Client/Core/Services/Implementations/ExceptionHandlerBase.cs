//-:cnd:noEmit
using System.Diagnostics;

namespace TodoTemplate.Client.Core.Services.Implementations;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    private readonly IStringLocalizer<AppStrings> _localizer = default!;
    private readonly IAuthenticationService _authenticationService = default!;
    private readonly MessageBoxService _messageBoxService = default!;

    public ExceptionHandlerBase(IStringLocalizer<AppStrings> localizer, IAuthenticationService authenticationService, MessageBoxService messageBoxService)
    {
        _localizer = localizer;
        _authenticationService = authenticationService;
        _messageBoxService = messageBoxService;
    }

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
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
