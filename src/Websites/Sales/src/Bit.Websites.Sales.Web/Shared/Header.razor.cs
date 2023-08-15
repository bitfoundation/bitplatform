namespace Bit.Websites.Sales.Web.Shared;

public partial class Header
{
    private string? _currentUrl;
    private bool _isMenuOpen;

    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IJSRuntime _js = default!;

    protected override void OnInitialized()
    {
        _navigationManager.LocationChanged += OnLocationChanged;

        SetCurrentUrl();

        base.OnInitialized();
    }

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        SetCurrentUrl();

        _ = CloseMenu();
    }

    private void SetCurrentUrl()
    {
        _currentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        var hashIndex = _currentUrl.IndexOf('#');
        if (hashIndex > 0)
        {
            _currentUrl = _currentUrl.Substring(0, hashIndex);
        }
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
