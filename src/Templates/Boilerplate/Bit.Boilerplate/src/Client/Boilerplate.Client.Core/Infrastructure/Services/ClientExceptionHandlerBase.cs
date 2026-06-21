//+:cnd:noEmit
using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Infrastructure.Services;

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
        parameters ??= [];

        parameters[nameof(filePath)] = filePath;
        parameters[nameof(memberName)] = memberName;
        parameters[nameof(lineNumber)] = lineNumber;

        Handle(exception, displayKind, parameters);
    }

    protected virtual void Handle(Exception exception,
        ExceptionDisplayKind displayKind,
        Dictionary<string, object?> parameters)
    {
        parameters = TelemetryContext.ToDictionary(parameters);

        foreach (var item in GetExceptionData(exception))
        {
            parameters[item.Key] = item.Value;
        }

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
        if (exception is TransientException)
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
