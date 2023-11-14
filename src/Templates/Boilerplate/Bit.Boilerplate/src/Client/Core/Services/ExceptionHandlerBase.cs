//-:cnd:noEmit
using System.Diagnostics;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected readonly IAuthenticationService AuthenticationService = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        if (exception is UnauthorizedException)
        {
            SignOut();

            return;
        }

#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = MessageBoxService.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
        _ = Console.Out.WriteLineAsync(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = MessageBoxService.Show(knownException.Message, Localizer[nameof(AppStrings.Error)]);
        }
        else
        {
            _ = MessageBoxService.Show(Localizer[nameof(AppStrings.UnknownException)], Localizer[nameof(AppStrings.Error)]);
        }
#endif
    }

    private async void SignOut()
    {
        try
        {
            await MessageBoxService.Show(Localizer[nameof(AppStrings.YouNeedToSignIn)], Localizer[nameof(AppStrings.Error)]);

            await AuthenticationService.SignOut();
        }
        catch (Exception exp)
        {
            Handle(exp);
        }
    }
}
