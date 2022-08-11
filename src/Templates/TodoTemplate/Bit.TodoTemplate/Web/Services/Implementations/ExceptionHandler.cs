//-:cnd:noEmit
using System.Diagnostics;
using TodoTemplate.App.Shared;

namespace TodoTemplate.App.Services.Implementations;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] IStringLocalizer<AppStrings> _localizer = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        MessageBox.Show(exceptionMessage, _localizer[nameof(AppStrings.Error)]);
        Console.WriteLine(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            MessageBox.Show(knownException.Message, _errorsLocalizer[nameof(AppStrings.Error)]);
        }
        else
        {
            MessageBox.Show(_errorsLocalizer[nameof(AppStrings.UnknownException)], _errorsLocalizer[nameof(AppStrings.Error)]);
        }
#endif

    }
}
