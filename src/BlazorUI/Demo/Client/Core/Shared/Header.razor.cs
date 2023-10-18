using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class Header
{
    private string _currentUrl = string.Empty;
    private bool _isHeaderMenuOpen;

    [AutoInject] private NavManuService _menuService { get; set; } = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;
    [AutoInject] private BitThemeManager _bitThemeManager { get; set; } = default!;

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

    private async Task ToggleTheme()
    {
        await _bitDeviceCoordinator.ApplyTheme(await _bitThemeManager.ToggleDarkLightAsync() == "dark");
    }
}
