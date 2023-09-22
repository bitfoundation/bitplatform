using Microsoft.AppCenter.Crashes;

namespace Bit.BlazorUI.Demo.Client.App.Services;

public partial class AppExceptionHandler : ExceptionHandlerBase
{
    public AppExceptionHandler(IStringLocalizer<AppStrings> localizer) 
        : base(localizer)
    {
    }

    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        Crashes.TrackError(exception, parameters?.ToDictionary(p => p.Key, p => p.Value?.ToString()));

        base.Handle(exception, parameters);
    }
}
