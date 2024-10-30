
namespace AdminPanel.Client.Maui.Services;

/// <summary>
/// Instead of Client.Core, install AppCenter, Firebase etc in Client.Maui, so the web version of the app won't download unnecessary packages.
/// You can call their APIs such as Crashes.TrackError in MauiExceptionHandler to monitor all exceptions across Android, iOS, Windows, and macOS.
/// Employing Microsoft.Extensions.Logging implementations (like Sentry.Extensions.Logging) will result in
/// automatic exception logging due to the logger.LogError method call within the ExceptionHandlerBase class.
/// </summary>
public partial class MauiExceptionHandler : ExceptionHandlerBase
{
    protected override void Handle(Exception exception, Dictionary<string, object> parameters)
    {
        base.Handle(exception, parameters);
    }
}
