using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Boilerplate.Workbench.Components.Preview;

/// <summary>
/// Renders a catalog page the way the router would, so its [Authorize] rules decide between the page and the app's
/// not-authorized page for the scenario's user.
/// </summary>
public partial class PreviewPageView
{
    private RouteData routeData = default!;
    private PreviewErrorBoundary? errorBoundary;

    [AutoInject] private BlexManager blexManager = default!;

    [Parameter] public WorkbenchPageInfo Page { get; set; } = default!;

    protected override void OnParametersSet()
    {
        // A new RouteData only for another page, so a re-render doesn't recreate the page.
        if (routeData?.PageType != Page.ComponentType)
        {
            routeData = new(Page.ComponentType, new Dictionary<string, object?>());
        }
    }

    /// <summary>A failed page still counts as settled, so the shell stops waiting for it and screenshots show the error.</summary>
    private async Task OnPageFailed()
    {
        // An exception thrown here would end the circuit, as the boundary rethrows what its handler throws.
        if (IsInsidePreview is false || IsPrerendering)
            return;

        try
        {
            await JSRuntime.InvokeVoidAsync("workbench.markReady");
        }
        catch (Exception exception) when (exception is JSDisconnectedException or JSException)
        {
        }
    }

    /// <summary>
    /// Puts the page's stores back to their defaults and renders it again. In a preview it also removes what the browser
    /// saved for this view; inline in the shell it keeps the shell's own store and its saved views.
    /// </summary>
    private async Task ResetView()
    {
        if (IsInsidePreview)
        {
            blexManager.ResetAll();
            await Persistor.ClearAsync();
        }
        else
        {
            foreach (var pageStore in blexManager.Stores.OfType<BlexStoreBase>().Where(s => s != WorkbenchStore))
            {
                pageStore.ResetState();
            }
        }

        errorBoundary?.Recover();
    }
}
