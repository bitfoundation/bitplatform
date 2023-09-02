using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Web.Shared;

public partial class MainLayout : IDisposable
{
    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    private bool _isTemplateDocRoute;

    protected override Task OnInitializedAsync()
    {
        SetCurrentUrl();

        NavigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();

        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        var currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        _isTemplateDocRoute = currentUrl.Contains("templates") || currentUrl.Contains("admin-panel") || currentUrl.Contains("todo-template");
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
