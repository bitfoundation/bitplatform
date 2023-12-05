using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        var isDebug = BuildConfiguration.IsDebug();

        string exceptionMessage = (exception as KnownException)?.Message ??
            (isDebug ? exception.ToString() : nameof(UnknownException));

        if (isDebug)
        {
            _ = Console.Out.WriteLineAsync(exceptionMessage);
            Debugger.Break();
        }

        _ = MessageBoxService.Show(exceptionMessage, "Error");
    }
}
