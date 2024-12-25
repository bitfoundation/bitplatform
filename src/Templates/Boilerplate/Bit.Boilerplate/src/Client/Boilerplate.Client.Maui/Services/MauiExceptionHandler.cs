
namespace Boilerplate.Client.Maui.Services;

/// <summary>
/// Instead of Client.Core, install Firebase and similar packages in Client.Maui, so the web version of the app won't download unnecessary packages.
/// You can call their APIs such as Crashes.TrackError in MauiExceptionHandler to monitor all exceptions across Android, iOS, Windows, and macOS.
/// Employing Microsoft.Extensions.Logging implementations (like Sentry.Extensions.Logging) will result in
/// automatic exception logging due to the logger.LogError method call within the ExceptionHandlerBase class.
/// </summary>
public partial class MauiExceptionHandler : ClientExceptionHandlerBase
{
    protected override void Handle(Exception exception, ExceptionDisplayKind displayKind, Dictionary<string, object> parameters)
    {
        exception = UnWrapException(exception);

        if (IgnoreException(exception))
            return;

        base.Handle(exception, displayKind, parameters);
    }
}
