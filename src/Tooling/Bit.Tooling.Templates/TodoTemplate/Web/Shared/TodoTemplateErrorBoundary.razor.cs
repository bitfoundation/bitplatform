namespace TodoTemplate.App.Shared;

public partial class TodoTemplateErrorBoundary
{
    private bool ShowException { get; set; }

    [Inject] private IExceptionHandler ExceptionHandler { get; set; } = default!;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

//-:cnd:noEmit
#if DEBUG
    protected override void OnInitialized()
    {
        ShowException = true;
    }
#endif
//+:cnd:noEmit

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
