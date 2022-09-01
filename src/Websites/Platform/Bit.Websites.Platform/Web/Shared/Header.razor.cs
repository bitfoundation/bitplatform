using System;
using System.Threading.Tasks;
using Bit.Websites.Platform.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Websites.Platform.Web.Shared;

public partial class Header : IDisposable
{
    private string CurrentUrl = string.Empty;
    private bool IsHeaderMenuOpen;

    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] public NavManuService _navManuService { get; set; }
    [AutoInject] public IJSRuntime _jsRuntime { get; set; }

    protected override void OnInitialized()
    {
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        _navigationManager.LocationChanged += OnLocationChanged;

        base.OnInitialized();
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    private void ToggleMenu()
    {
        _navManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (CurrentUrl.Contains("admin-panel") || CurrentUrl.Contains("todo-template"))
        {
            return "Prodocus & Services";
        }
        else return CurrentUrl switch
        {
            Urls.HomePage => "Home",
            Urls.Components => "Prodocus & Services",
            Urls.CloudHostringSolutins => "Prodocus & Services",
            Urls.Support => "Prodocus & Services",
            Urls.Academy => "Prodocus & Services",
            Urls.Pricing => "Pricing",
            Urls.AboutUs => "About us",
            Urls.ContactUs => "Contact us",
            Urls.Blogs => "Blogs",
            Urls.Videos => "Videos",
            _ => "Prodocus & Services",
        };
    }

    private bool IsProductsServicesActive()
    {
        return (CurrentUrl.Contains("admin-panel") ||
           CurrentUrl.Contains("todo-template") ||
           CurrentUrl == Urls.Components ||
           CurrentUrl == Urls.CloudHostringSolutins ||
           CurrentUrl == Urls.Support ||
           CurrentUrl == Urls.Academy);
    }

    private async Task ToggleHeaderMenu()
    {
        IsHeaderMenuOpen = !IsHeaderMenuOpen;
        await _jsRuntime.SetToggleBodyOverflow(IsHeaderMenuOpen);
        StateHasChanged();
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
