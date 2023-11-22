//-:cnd:noEmit
using System.Diagnostics;

namespace BlazorWeb.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] IStringLocalizer<AppStrings> _localizer = default!;
    [AutoInject] MessageBoxService _messageBoxService = default!;

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

        _ = _messageBoxService.Show(exceptionMessage, _localizer[nameof(AppStrings.Error)]);
    }
}
