using Bit.BlazorUI.Demo.Client.Shared.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Shared;

public partial class Header
{
    private string _currentUrl = string.Empty;
    private bool _isHeaderMenuOpen;
    private bool _isDarkMode;

    [Inject] private NavManuService _menuService { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private async Task ToggleNavMenu()
    {
        _isHeaderMenuOpen = false;
        await _menuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (_currentUrl.Contains("components"))
        {
            return "Docs";
        }
        else return _currentUrl switch
        {
            "/" => "Home",
            _ => "Docs",
        };
    }

    private async Task ToggleHeaderMenu()
    {
        try
        {
            _isHeaderMenuOpen = !_isHeaderMenuOpen;

            await JSRuntime.ToggleBodyOverflow(_isHeaderMenuOpen);
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task ToggleTheme(bool value)
    {
        _isDarkMode = !_isDarkMode;
        await JSRuntime.ToggleBitTheme(_isDarkMode);
    }
}
