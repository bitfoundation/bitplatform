namespace Boilerplate.Client.Core.Components;

public partial class AppErrorBoundary
{
    private BitErrorBoundary boundaryRef = default!;
    private string? message;


    [Parameter] public RenderFragment? ChildContent { get; set; }


    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;


    /// <summary>
    /// The boundary's own page is where a caught exception is shown: it has taken the place of the layout, and with it of the
    /// modal and snack bar containers, so a message box or a snack bar would have nowhere to render. A known exception's
    /// message is written for the user, so it goes under the title; any other is shown in full in development only.
    /// </summary>
    private void HandleError(Exception exception)
    {
        exception = exceptionHandler.UnWrapException(exception);

        message = exception is KnownException ? exception.Message : null;

        exceptionHandler.Handle(exception, ExceptionDisplayKind.None);
    }

    private async Task ShowDiagnostic()
    {
        boundaryRef.Recover();
        await Task.Yield();
        pubSubService.Publish(ClientAppMessages.SHOW_DIAGNOSTIC_MODAL);
    }
}
