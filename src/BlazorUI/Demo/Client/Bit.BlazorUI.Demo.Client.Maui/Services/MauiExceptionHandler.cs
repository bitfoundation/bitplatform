using Microsoft.AppCenter.Crashes;

namespace Bit.BlazorUI.Demo.Client.Maui.Services;

/// <summary>
/// You can easily install AppCenter, Firebase Crashlytics, and other exception tracking libraries in your Client.Maui project.
/// Then, you can use their APIs to monitor all exceptions across Android, iOS, Windows, and macOS.
/// </summary>
public partial class MauiExceptionHandler : ExceptionHandlerBase
{
    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        if (exception is TaskCanceledException)
        {
            return;
        }

        Crashes.TrackError(exception, parameters?.ToDictionary(p => p.Key, p => p.Value?.ToString()));

        base.Handle(exception, parameters);
    }
}
