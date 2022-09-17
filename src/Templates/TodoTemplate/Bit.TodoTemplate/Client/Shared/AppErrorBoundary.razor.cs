//-:cnd:noEmit
namespace TodoTemplate.Client.Shared;

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

    protected override async Task OnErrorAsync(Exception exception)
    {
        _exceptionHandler.Handle(exception);
    }

    private void Refresh()
    {
        _navigationManager.NavigateTo(_navigationManager.Uri, true);
    }

    private void GoHome()
    {
        _navigationManager.NavigateTo("/", true);
    }
}
