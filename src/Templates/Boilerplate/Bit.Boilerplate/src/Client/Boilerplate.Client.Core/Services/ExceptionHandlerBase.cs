//+:cnd:noEmit
using System.Reflection;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected Bit.Butil.Console Console = default!;
    [AutoInject] protected ITelemetryContext TelemetryContext = default!;
    [AutoInject] protected ILogger<ExceptionHandlerBase> Logger = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;

    public void Handle(Exception exception,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        parameters = TelemetryContext.ToDictionary(parameters);

        parameters[nameof(filePath)] = filePath;
        parameters[nameof(memberName)] = memberName;
        parameters[nameof(lineNumber)] = lineNumber;

        Handle(exception, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        var isDevEnv = AppEnvironment.IsDev();

        using (var scope = Logger.BeginScope(parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty)))
        {
            var exceptionMessageToLog = exception.Message;
            var innerException = exception.InnerException;

            while (innerException is not null)
            {
                exceptionMessageToLog += $"{Environment.NewLine}{innerException.Message}";
                innerException = innerException.InnerException;
            }

            if (exception is KnownException)
            {
                Logger.LogError(exception, exceptionMessageToLog);
            }
            else
            {
                Logger.LogCritical(exception, exceptionMessageToLog);
            }
        }

        string exceptionMessageToShow = (exception as KnownException)?.Message ??
            (isDevEnv ? exception.ToString() : Localizer[nameof(AppStrings.UnknownException)]);

        MessageBoxService.Show(exceptionMessageToShow, Localizer[nameof(AppStrings.Error)]);

        if (isDevEnv)
        {
            Debugger.Break();
        }
    }

    protected Exception UnWrapException(Exception exception)
    {
        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerException ?? aggregateException;
        }
        else if (exception is TargetInvocationException)
        {
            return exception.InnerException ?? exception;
        }

        return exception;
    }

    protected bool IgnoreException(Exception exception)
    {
        if (exception is KnownException)
            return false;

        return exception is TaskCanceledException ||
            exception is OperationCanceledException ||
            exception is TimeoutException ||
            (exception.InnerException is not null && IgnoreException(exception.InnerException));
    }
}
