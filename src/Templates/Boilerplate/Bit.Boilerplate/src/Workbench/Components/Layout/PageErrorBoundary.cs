using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Workbench.Components.Layout;

/// <summary>
/// An error boundary around a whole page: the error replaces the page until another page opens, whatever renders in the meantime.
/// </summary>
public class PageErrorBoundary : ErrorBoundary, IDisposable
{
    private string path = string.Empty;

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        path = PathOf(NavigationManager.Uri);

        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var newPath = PathOf(args.Location);

        if (newPath == path)
            return;

        path = newPath;

        _ = InvokeAsync(Recover);
    }

    private static string PathOf(string uri) => new Uri(uri).AbsolutePath;

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
