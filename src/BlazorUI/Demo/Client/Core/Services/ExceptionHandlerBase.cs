using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public abstract partial class ExceptionHandlerBase : IExceptionHandler
{
    [AutoInject] protected readonly IStringLocalizer<AppStrings> Localizer = default!;

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = MessageBox.Show(exceptionMessage, Localizer[nameof(AppStrings.Error)]);
        Console.WriteLine(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = MessageBox.Show(knownException.Message, Localizer[nameof(AppStrings.Error)]);
        }
        else
        {
            _ = MessageBox.Show(Localizer[nameof(AppStrings.UnknownException)], Localizer[nameof(AppStrings.Error)]);
        }
#endif
    }
}
