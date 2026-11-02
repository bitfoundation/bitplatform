namespace Boilerplate.Workbench.Components.Layout;

/// <summary>
/// What a layout shows in place of a page that threw: a short message and a button that loads the page again.
/// </summary>
public partial class LayoutError
{
    /// <summary>
    /// Tells the shell the preview has settled, so its view drops the loading ring and a screenshot doesn't wait for a
    /// page that will never finish.
    /// </summary>
    [Parameter] public bool MarkReady { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender is false || MarkReady is false)
            return;

        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.markReady");
        }
        catch (JSException)
        {
            // The page has failed already, and nothing else waits on this call.
        }
    }

    private void Reload() => NavigationManager.Refresh(forceReload: true);
}
