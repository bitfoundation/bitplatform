﻿//+:cnd:noEmit
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ClientExceptionHandlerBase : SharedExceptionHandler, IExceptionHandler
{
    [AutoInject] protected readonly SnackBarService SnackBarService = default!;
    [AutoInject] protected readonly ITelemetryContext TelemetryContext = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;
    [AutoInject] protected readonly ILogger<ClientExceptionHandlerBase> Logger = default!;

    public void Handle(Exception exception,
        ExceptionDisplayKind displayKind = ExceptionDisplayKind.Interrupting,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        parameters = TelemetryContext.ToDictionary(parameters);

        parameters[nameof(filePath)] = filePath;
        parameters[nameof(memberName)] = memberName;
        parameters[nameof(lineNumber)] = lineNumber;
        parameters["exceptionId"] = Guid.NewGuid(); // This will remain consistent across different registered loggers, such as Sentry, Application Insights, etc.

        Handle(exception, displayKind, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void Handle(Exception exception,
        ExceptionDisplayKind displayKind,
        Dictionary<string, object> parameters)
    {
        var isDevEnv = AppEnvironment.IsDev();

        using (var scope = Logger.BeginScope(parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty)))
        {
            var exceptionMessageToLog = GetExceptionMessageToLog(exception);

            if (exception is KnownException)
            {
                Logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                Logger.LogCritical(exception, exceptionMessageToLog);
            }
        }

        string exceptionMessageToShow = GetExceptionMessageToShow(exception);

        if (displayKind is ExceptionDisplayKind.NonInterrupting)
        {
            SnackBarService.Error("Boilerplate", exceptionMessageToShow);
        }
        else if (displayKind is ExceptionDisplayKind.Interrupting)
        {
            MessageBoxService.Show(exceptionMessageToShow, Localizer[nameof(AppStrings.Error)]);
        }
        else if (displayKind is ExceptionDisplayKind.None && isDevEnv)
        {
            Debugger.Break();
        }
    }
}
