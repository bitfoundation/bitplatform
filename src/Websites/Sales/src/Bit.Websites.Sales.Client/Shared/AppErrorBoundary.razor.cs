
namespace Bit.Websites.Sales.Client.Shared;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors
/// </summary>
public partial class AppErrorBoundary
{
    private bool showException;

    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

#if DEBUG
    protected override void OnInitialized()
    {
        showException = true;
    }
#endif

    protected override async Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);
    }

    private void Refresh()
    {
        navigationManager.Refresh(forceReload: true);
    }

    private void GoHome()
    {
        navigationManager.NavigateTo("/", true);
    }
}
