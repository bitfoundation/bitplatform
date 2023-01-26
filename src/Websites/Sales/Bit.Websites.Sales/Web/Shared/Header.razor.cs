namespace Bit.Websites.Sales.Web.Shared;

public partial class Header
{
    private string? _currentUrl;
    private bool _isMenuOpen;

    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IJSRuntime _js = default!;

    protected override void OnInitialized()
    {
        _currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _navigationManager.LocationChanged += OnLocationChanged;
        base.OnInitialized();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        _currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _ = CloseMenu();
    }

    private async Task OpenMenu()
    {
        _isMenuOpen = true;
        await _js.InvokeVoidAsync("App.setBodyStyle", "overflow:hidden;");
        StateHasChanged();
    }

    private async Task CloseMenu()
    {
        _isMenuOpen = false;
        await _js.InvokeVoidAsync("App.setBodyStyle", "overflow:auto;");
        StateHasChanged();
    }
}
