//+:cnd:noEmit
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
        if (IgnoreException(exception))
            return;

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
            var exceptionMessage = exception.Message;
            var innerException = exception.InnerException;

            while (innerException is not null)
            {
                exceptionMessage += $"{Environment.NewLine}{innerException.Message}";
                innerException = innerException.InnerException;
            }

            Logger.LogError(exception, exceptionMessage);
        }

        string displayableExceptionMessage = (exception as KnownException)?.Message ??
            (isDevEnv ? exception.ToString() : Localizer[nameof(AppStrings.UnknownException)]);

        MessageBoxService.Show(displayableExceptionMessage, Localizer[nameof(AppStrings.Error)]);

        if (isDevEnv)
        {
            Debugger.Break();
        }
    }

    protected bool IgnoreException(Exception exception)
    {
        return exception is TaskCanceledException;
    }
}
