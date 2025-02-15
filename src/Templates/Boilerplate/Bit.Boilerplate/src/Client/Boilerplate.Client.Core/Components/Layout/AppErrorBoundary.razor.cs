namespace Boilerplate.Client.Core.Components.Layout;

public partial class AppErrorBoundary
{
    private BitErrorBoundary boundaryRef = default!;


    [Parameter] public RenderFragment? ChildContent { get; set; }


    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;


    private async Task ShowDiagnostic()
    {
        boundaryRef.Recover();
        await Task.Yield();
        pubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
}
