namespace Bit.BlazorUI;

public partial class BitErrorBoundary : ErrorBoundaryBase
{
    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The callback for when an error get caught by the boundary.
    /// </summary>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool ShowException { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }



    protected override async Task OnErrorAsync(Exception exception)
    {
        await OnError.InvokeAsync(exception);
    }


    private void Refresh()
    {
        _navigationManager.Refresh(forceReload: true);
    }

    private void GoHome()
    {
        _navigationManager.NavigateTo("/", forceLoad: true);
    }
}
