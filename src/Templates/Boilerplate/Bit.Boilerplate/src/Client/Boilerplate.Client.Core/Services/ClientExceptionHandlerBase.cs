//+:cnd:noEmit
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ClientExceptionHandlerBase : SharedExceptionHandler, IExceptionHandler
{
    [AutoInject] protected readonly PubSubService PubSubService = default!;
    [AutoInject] protected readonly SnackBarService SnackBarService = default!;
    [AutoInject] protected readonly ITelemetryContext TelemetryContext = default!;
    [AutoInject] protected readonly BitMessageBoxService MessageBoxService = default!;
    [AutoInject] protected readonly ILogger<ClientExceptionHandlerBase> Logger = default!;

    public void Handle(Exception exception,
        ExceptionDisplayKind displayKind = ExceptionDisplayKind.Default,
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

        foreach (var item in GetExceptionData(exception))
        {
            parameters[item.Key] = item.Value;
        }

        Handle(exception, displayKind, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void Handle(Exception exception,
        ExceptionDisplayKind displayKind,
        Dictionary<string, object> parameters)
    {
        var isDevEnv = AppEnvironment.IsDevelopment();

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

        if (displayKind is ExceptionDisplayKind.Default)
        {
            displayKind = GetDisplayKind(exception);
        }

        if (displayKind is ExceptionDisplayKind.NonInterrupting)
        {
            SnackBarService.Error("Boilerplate", exceptionMessageToShow);
        }
        else if (displayKind is ExceptionDisplayKind.Interrupting)
        {
            _ = MessageBoxService.Show(Localizer[nameof(AppStrings.Error)], exceptionMessageToShow);
        }
        else if (displayKind is ExceptionDisplayKind.None && isDevEnv)
        {
            Debugger.Break();
        }
    }

    private ExceptionDisplayKind GetDisplayKind(Exception exception)
    {
        if (exception is ServerConnectionException)
            return ExceptionDisplayKind.NonInterrupting;

        if (exception is UnauthorizedException)
            return ExceptionDisplayKind.NonInterrupting;

        return ExceptionDisplayKind.Interrupting;
    }

    public override bool IgnoreException(Exception exception)
    {
        return exception is TaskCanceledException ||
            exception is OperationCanceledException ||
            exception is TimeoutException || base.IgnoreException(exception);
    }
}
