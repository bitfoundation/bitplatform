namespace AdminPanel.Client.Web.Services.Implementations;

public class WebExceptionHandler : ExceptionHandlerBase
{
    public WebExceptionHandler(IStringLocalizer<AppStrings> localizer, IAuthenticationService authenticationService, MessageBoxService messageBoxService)
    : base(localizer, authenticationService, messageBoxService)
    {
    }

    public override void Handle(Exception exception, IDictionary<string, object?>? parameters = null)
    {
        base.Handle(exception, parameters);
    }
}
