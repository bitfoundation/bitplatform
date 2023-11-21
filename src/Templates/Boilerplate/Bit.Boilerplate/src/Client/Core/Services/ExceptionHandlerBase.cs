//-:cnd:noEmit
using System.Diagnostics;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
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

        _ = MessageBoxService.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
    }
}
