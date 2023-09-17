namespace AdminPanel.Client.App.Services.Implementations;

/// <summary>
/// You can easily install AppCenter, Firebase Crashlytics, and other exception tracking libraries in your Client.App project.
/// Then, you can use their APIs to monitor all exceptions across Android, iOS, Windows, and macOS.
/// </summary>
public class AppExceptionHandler : ExceptionHandlerBase
{
    protected AppExceptionHandler(IStringLocalizer<AppStrings> localizer, IAuthenticationService authenticationService, MessageBoxService messageBoxService)
        : base(localizer, authenticationService, messageBoxService)
    {

    }

    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.Handle(exception, parameters);
    }
}
