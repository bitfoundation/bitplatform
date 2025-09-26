using System.Diagnostics;

namespace Bit.Websites.Platform.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] private MessageBoxService messageBoxService = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = Console.Out.WriteLineAsync(exceptionMessage);
#if DEBUG
        _ = messageBoxService.Show(exceptionMessage, "Error");
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
