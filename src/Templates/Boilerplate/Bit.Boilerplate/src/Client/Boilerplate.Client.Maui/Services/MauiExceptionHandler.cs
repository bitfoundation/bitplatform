
namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// You can easily install AppCenter, Firebase Crashlytics, and other exception tracking libraries in your Client.Maui project.
/// Then, you can use their APIs to monitor all exceptions across Android, iOS, Windows, and macOS.
/// </summary>
public partial class MauiExceptionHandler : ExceptionHandlerBase
{
    protected override void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        base.Handle(exception, parameters);
    }
}
