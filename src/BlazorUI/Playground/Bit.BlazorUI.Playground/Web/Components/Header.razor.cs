using System;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Playground.Web.Components;

public partial class Header
{
    private string CurrentUrl = string.Empty;
    private bool IsHeaderMenuOpen;

    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public NavManuService NavManuService { get; set; }

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

    private async Task ToggleMenu()
    {
        await NavManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        return CurrentUrl switch
        {
            "/" => "Home",
            "/components/overview" => "Demo",
            "/get-started" => "Get Started",
            "/icons" => "Iconography",
            _ => "",
        };
    }

    private void ToggleHeaderMenu()
    {
        IsHeaderMenuOpen = !IsHeaderMenuOpen;
    }
}
