//-:cnd:noEmit
namespace BlazorWeb.Web.Shared;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/handle-errors
/// </summary>
public partial class AppErrorBoundary
{
    [AutoInject] private IExceptionHandler _exceptionHandler = default!;

    [AutoInject] private NavigationManager _navigationManager = default!;

    private bool ShowException { get; set; }

#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif

    protected override Task OnErrorAsync(Exception exception)
    {
        _exceptionHandler.Handle(exception);

        return Task.CompletedTask;
    }

    private void Refresh()
    {
        _navigationManager.Refresh(forceReload: true);
    }

    private void GoHome()
    {
        _navigationManager.NavigateTo("/", true);
    }
}
