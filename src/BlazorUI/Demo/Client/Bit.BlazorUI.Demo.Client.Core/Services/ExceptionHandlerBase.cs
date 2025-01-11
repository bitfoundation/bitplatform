using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly BitMessageBoxService MessageBoxService = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        var isDebug = BuildConfiguration.IsDebug();

        string exceptionMessage = (exception as KnownException)?.Message ??
            (isDebug ? exception.ToString() : nameof(UnknownException));

        if (isDebug)
        {
            Debugger.Break();
        }

        _ = Console.Out.WriteLineAsync(exception.ToString());

        _ = MessageBoxService.Show("Error", exceptionMessage);
    }
}
