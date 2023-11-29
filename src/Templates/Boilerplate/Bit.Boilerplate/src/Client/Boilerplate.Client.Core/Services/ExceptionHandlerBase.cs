//-:cnd:noEmit
using System.Diagnostics;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        var isDebug = BuildConfigurationModeDetector.Current.IsDebug();

        string exceptionMessage = (exception as KnownException)?.Message ??
            (isDebug ? exception.ToString() : Localizer[nameof(AppStrings.UnknownException)]);

        if (isDebug)
        {
            _ = Console.Out.WriteLineAsync(exceptionMessage);
            Debugger.Break();
        }

        _ = MessageBoxService.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
    }
}
