﻿using Bit.BlazorUI.Demo.Client.Shared.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Shared;

public partial class Header
{
    private string CurrentUrl = string.Empty;
    private bool IsHeaderMenuOpen;

    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public NavManuService NavManuService { get; set; }
    [Inject] public IJSRuntime JsRuntime { get; set; }
    [Inject] private IExceptionHandler exceptionHandler { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private async Task ToggleNavMenu()
    {
        IsHeaderMenuOpen = false;
        await NavManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (CurrentUrl.Contains("components"))
        {
            return "Docs";
        }
        else return CurrentUrl switch
        {
            "/" => "Home",
            _ => "Docs",
        };
    }

    private async Task ToggleHeaderMenu()
    {
        try
        {
            IsHeaderMenuOpen = !IsHeaderMenuOpen;

            await JsRuntime.ToggleBodyOverflow(IsHeaderMenuOpen);
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }
}
