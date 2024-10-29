//-:cnd:noEmit
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

    public void Handle(Exception exp,
        Dictionary<string, object?>? parameters = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "")
    {
        if (exp is TaskCanceledException)
            return;

        parameters ??= [];

        parameters[nameof(filePath)] = filePath;
        parameters[nameof(memberName)] = memberName;
        parameters[nameof(lineNumber)] = lineNumber;
        parameters[nameof(TelemetryContext.UserId)] = TelemetryContext.UserId;
        parameters[nameof(TelemetryContext.UserSessionId)] = TelemetryContext.UserSessionId;
        parameters[nameof(TelemetryContext.AppSessionId)] = TelemetryContext.AppSessionId;
        parameters[nameof(TelemetryContext.AppVersion)] = TelemetryContext.AppVersion;
        parameters[nameof(TelemetryContext.OS)] = TelemetryContext.OS;
        if (AppPlatform.IsBlazorHybrid)
        {
            parameters[nameof(TelemetryContext.WebView)] = TelemetryContext.WebView;
        }

        Handle(exp, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        var isDevEnv = AppEnvironment.IsDev();

        string exceptionMessage = (exception as KnownException)?.Message ??
            (isDevEnv ? exception.ToString() : Localizer[nameof(AppStrings.UnknownException)]);

        if (isDevEnv)
        {
            Debugger.Break();
        }

        using (var scope = Logger.BeginScope(parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty)))
        {
            Logger.LogError(exception, exceptionMessage);
        }

        MessageBoxService.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
    }
}
