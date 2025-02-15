namespace Bit.BlazorUI;

/// <summary>
/// BitErrorBoundary is a simple error boundary to handle exceptions happening in its children.
/// </summary>
public partial class BitErrorBoundary : ErrorBoundaryBase
{
    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The footer content of the boundary.
    /// </summary>
    [Parameter] public RenderFragment? AdditionalButtons { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Body { get; set; }

    /// <summary>
    /// The footer content of the boundary.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the Home button.
    /// </summary>
    [Parameter] public string? HomeText { get; set; }

    /// <summary>
    /// The url of the home page for the Home button.
    /// </summary>
    [Parameter] public string? HomeUrl { get; set; }

    /// <summary>
    /// The callback for when an error get caught by the boundary.
    /// </summary>
    [Parameter] public EventCallback<Exception> OnError { get; set; }

    /// <summary>
    /// The text of the Recover button.
    /// </summary>
    [Parameter] public string? RecoverText { get; set; }

    /// <summary>
    /// The text of the Refresh button.
    /// </summary>
    [Parameter] public string? RefreshText { get; set; }

    /// <summary>
    /// Whether the actual exception information should be shown or not.
    /// </summary>
    [Parameter] public bool ShowException { get; set; }

    /// <summary>
    /// The header title of the boundary.
    /// </summary>
    [Parameter] public string? Title { get; set; }



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
        _navigationManager.NavigateTo(HomeUrl ?? "/", forceLoad: true);
    }
}
