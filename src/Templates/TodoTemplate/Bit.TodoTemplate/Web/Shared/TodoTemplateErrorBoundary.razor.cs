//-:cnd:noEmit
namespace TodoTemplate.App.Shared;

public partial class TodoTemplateErrorBoundary
{
    [AutoInject] private IExceptionHandler exceptionHandler = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

    private bool ShowException { get; set; }

#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif

    protected override Task OnErrorAsync(Exception exception)
    {
        exceptionHandler.Handle(exception);

        return Task.CompletedTask;
    }

    private void Refresh()
    {
        navigationManager.NavigateTo(navigationManager.Uri, true);
    }

    private void GoHome()
    {
        navigationManager.NavigateTo("/", true);
    }
}
