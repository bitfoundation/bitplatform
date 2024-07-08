//-:cnd:noEmit
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected readonly MessageBoxService MessageBoxService = default!;
    [AutoInject] protected Bit.Butil.Console Console = default!;
    [AutoInject] protected ILogger<ExceptionHandlerBase> Logger = default!;

    public void Handle(Exception exp, IDictionary<string, object?>? parameters = null)
    {
        if (exp is TaskCanceledException)
            return;

        parameters ??= new Dictionary<string, object?>();

        Handle(exp, parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty));
    }

    protected virtual void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        var isDebug = AppEnvironment.IsDevelopment();

        string exceptionMessage = (exception as KnownException)?.Message ??
            (isDebug ? exception.ToString() : Localizer[nameof(AppStrings.UnknownException)]);

        if (isDebug)
        {
            if (AppRenderMode.IsBlazorHybrid)
            {
                StringBuilder errorInfo = new();
                errorInfo.AppendLine(exceptionMessage);
                foreach (var item in parameters)
                {
                    errorInfo.AppendLine($"{item.Key}: {item.Value}");
                }
                _ = Console.Error(errorInfo.ToString());
            }
            Debugger.Break();
        }

        using (var scope = Logger.BeginScope(parameters.ToDictionary(i => i.Key, i => i.Value ?? string.Empty)))
        {
            Logger.LogError(exception, exceptionMessage);
        }

        _ = MessageBoxService.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
    }
}
