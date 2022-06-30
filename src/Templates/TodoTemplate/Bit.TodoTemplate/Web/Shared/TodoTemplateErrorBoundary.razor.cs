//-:cnd:noEmit
namespace TodoTemplate.App.Shared;

public partial class TodoTemplateErrorBoundary
{
    private bool ShowException { get; set; }

    [AutoInject] private IExceptionHandler ExceptionHandler = default!;

    [AutoInject] private NavigationManager NavigationManager = default!;


#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif

    protected override Task OnErrorAsync(Exception exception)
    {
        ExceptionHandler.Handle(exception);

        return Task.CompletedTask;
    }

    private void Refresh()
    {
        NavigationManager.NavigateTo(NavigationManager.Uri, true);
    }

    private void GoHome()
    {
        NavigationManager.NavigateTo("/", true);
    }
}
