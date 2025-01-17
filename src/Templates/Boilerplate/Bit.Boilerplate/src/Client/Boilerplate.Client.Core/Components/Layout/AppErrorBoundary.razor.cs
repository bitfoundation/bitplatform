//-:cnd:noEmit

namespace Boilerplate.Client.Core.Components.Layout;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors
/// </summary>
public partial class AppErrorBoundary
{
    private bool showException;

    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;

    protected override void OnInit()
    {
        showException = AppEnvironment.IsDev();
    }

    private async Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);
    }

    private async Task ShowDiagnostic()
    {
        //Recover();
        await Task.Yield();
        pubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }
}
