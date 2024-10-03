using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout.Main;

public partial class NavMenu : IDisposable
{
    private string? currentUrl;

    protected override Task OnInitAsync()
    {
        SetCurrentItem();

        NavigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        SetCurrentItem();
        StateHasChanged();
    }

    private void SetCurrentItem()
    {
        currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
    }

    private bool IsActive(string url)
    {
        return currentUrl == url;
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
