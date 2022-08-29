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

    private string GetHeaderLinkClass(string link)
    {
        var classStr = "header-link";
        if ((link == "Home" && CurrentUrl == "/") || (link == "Templates" && CurrentUrl.Contains("template")))
        {
            classStr += " header-link--active";
        }

        return classStr;
    }

    private string GetHeaderNavLinkClass(string link)
    {
        var classStr = "header-nav-link";
        if ((link == "Home" && CurrentUrl == "/") || (link == "Templates" && CurrentUrl.Contains("template")))
        {
            classStr += " header-nav-link--active";
        }

        return classStr;
    }

    private void ToggleMenu()
    {
        _navManuService.ToggleMenu();
    }

    private string GetActiveRouteName()
    {
        if (CurrentUrl.Contains("template"))
        {
            return "Templates";
        }
        else return CurrentUrl switch
        {
            "/" => "Home",
            _ => "Templates",
        };
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
