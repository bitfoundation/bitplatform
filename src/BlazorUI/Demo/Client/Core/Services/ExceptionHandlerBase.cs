using System.Diagnostics;

namespace Bit.BlazorUI.Demo.Client.Core.Services;

public abstract class ExceptionHandlerBase : IExceptionHandler
{
    private readonly IStringLocalizer<AppStrings> _localizer = default!;

    protected ExceptionHandlerBase(IStringLocalizer<AppStrings> localizer)
    {
        _localizer = localizer;
    }

    public virtual void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
#if DEBUG
        string exceptionMessage = (exception as KnownException)?.Message ?? exception.ToString();
        _ = MessageBox.Show(exceptionMessage, _localizer[nameof(AppStrings.Error)]);
        Console.WriteLine(exceptionMessage);
        Debugger.Break();
#else
        if (exception is KnownException knownException)
        {
            _ = MessageBox.Show(knownException.Message, _localizer[nameof(AppStrings.Error)]);
        }
        else
        {
            _ = MessageBox.Show(_localizer[nameof(AppStrings.UnknownException)], _localizer[nameof(AppStrings.Error)]);
        }
#endif
    }
}
