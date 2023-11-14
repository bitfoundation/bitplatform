﻿using System.Diagnostics;

namespace Bit.Websites.Platform.Client.Services;

public partial class ExceptionHandler : IExceptionHandler
{
    [AutoInject] MessageBoxService _messageBoxService = default!;

    public void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = _messageBoxService.Show(exceptionMessage, "Error");
        _ = Console.Out.WriteLineAsync(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = _messageBoxService.Show(knownException.Message, "Error");
        }
        else
        {
            _ = _messageBoxService.Show("Unknown error", "Error");
        }
#endif
    }
}
