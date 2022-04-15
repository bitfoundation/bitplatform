using Microsoft.AspNetCore.Components.Web;

namespace TodoTemplate.App.Shared;
public partial class ErrorBoundaryContent
{
    private bool ShowException = false;

    [Inject] private IExceptionHandler ExceptionHandler { get; set; } = default!;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Parameter] public RenderFragment<Exception?>? ChildContent { get; set; }
    
    [Parameter] public Exception? Exception { get; set; }

    protected override void OnInitialized()
    {
#if DEBUG
        ShowException = true;
#endif

        if (Exception is not null)
        {
            ExceptionHandler.Handle(Exception);
        }

        base.OnInitialized();
    }

    private void Refresh()
    {
        NavigationManager.NavigateTo(NavigationManager.Uri, true);
    }
}
