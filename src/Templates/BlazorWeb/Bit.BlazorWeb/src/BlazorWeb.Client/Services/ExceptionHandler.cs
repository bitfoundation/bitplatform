//-:cnd:noEmit
using System.Diagnostics;

namespace BlazorWeb.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] MessageBoxService messageBoxService = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        string exceptionMessage = (exception as KnownException)?.Message ??
#if DEBUG
            exception.ToString();
#else
            _localizer[nameof(AppStrings.UnknownException)];
#endif

#if DEBUG
        _ = Console.Out.WriteLineAsync(exceptionMessage);
        Debugger.Break();
#endif

        _ = messageBoxService.Show(exceptionMessage, localizer[nameof(AppStrings.Error)]);
    }
}
