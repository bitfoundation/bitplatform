using System;
using System.Threading.Tasks;
using Bit.BlazorUI.Playground.Web.Services;
using Bit.BlazorUI.Playground.Web.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Bit.BlazorUI.Playground.Web.Components;

public partial class Header
{
    private string CurrentUrl = string.Empty;
    private bool IsHeaderMenuOpen;

    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public NavManuService NavManuService { get; set; }
    [Inject] public IJSRuntime JsRuntime { get; set; }
    [Inject] private IExceptionHandler exceptionHandler { get; set; } = default!;

    protected override void OnInitialized()
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        base.OnInitialized();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private void ToggleMenu()
    {
        NavManuService.ToggleMenu();
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

    private async Task ToggleHeaderMenu()
    {
        try
        {
            IsHeaderMenuOpen = !IsHeaderMenuOpen;

            await JsRuntime.SetToggleBodyOverflow(IsHeaderMenuOpen);
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
