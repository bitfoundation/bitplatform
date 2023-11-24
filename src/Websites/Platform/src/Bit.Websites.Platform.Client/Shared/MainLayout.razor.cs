using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Websites.Platform.Client.Shared;

public partial class MainLayout : IDisposable
{
    [AutoInject] private NavigationManager navigationManager = default!;

    private bool isTemplateDocRoute;

    protected override Task OnInitializedAsync()
    {
        SetCurrentUrl();

        navigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();

        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        var currentUrl = navigationManager.Uri.Replace(navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        isTemplateDocRoute = currentUrl.Contains("templates") || currentUrl.Contains("admin-panel") || currentUrl.Contains("todo-template");
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
