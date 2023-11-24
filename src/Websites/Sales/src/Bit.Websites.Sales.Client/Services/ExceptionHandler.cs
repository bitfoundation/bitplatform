using System.Diagnostics;

namespace Bit.Websites.Sales.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] private MessageBoxService messageBoxService = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = messageBoxService.Show(exceptionMessage, "Error");
        _ = Console.Out.WriteLineAsync(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = messageBoxService.Show(knownException.Message, "Error");
        }
        else
        {
            _ = messageBoxService.Show("Unknown error", "Error");
        }
#endif
    }
}
