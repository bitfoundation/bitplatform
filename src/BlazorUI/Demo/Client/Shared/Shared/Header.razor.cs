﻿using Bit.BlazorUI.Demo.Client.Shared.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Shared;

public partial class Header
{
    private string _currentUrl = string.Empty;
    private bool _isHeaderMenuOpen;
    private bool _isDarkMode;

    [AutoInject] private NavManuService _menuService { get; set; } = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        _currentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);

        NavigationManager.LocationChanged += OnLocationChanged;

#if BlazorHybrid
        _isDarkMode = await JSRuntime.IsSystemThemeDark();
#endif

        await base.OnInitAsync();
    }

#if !BlazorHybrid
    protected override async Task OnAfterFirstRenderAsync()
    {
        _isDarkMode = await JSRuntime.IsSystemThemeDark();
        await base.OnAfterFirstRenderAsync();
    }
#endif

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
        _isDarkMode = !_isDarkMode;
        await JSRuntime.ToggleBitTheme(_isDarkMode);
#if BlazorHybrid
        _bitDeviceCoordinator.SetDeviceTheme(_isDarkMode);
#endif
    }
}
