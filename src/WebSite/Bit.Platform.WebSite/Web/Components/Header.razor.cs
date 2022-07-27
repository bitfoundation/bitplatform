using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Platform.WebSite.Web.Components;

public partial class Header : IDisposable
{
    private string CurrentUrl = string.Empty;

    [AutoInject] private NavigationManager _navigationManager = default!;

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
        if ((link == "Home" && CurrentUrl == "/") || (link == "Todo" && CurrentUrl == "/todo-template"))
        {
            classStr += " header-link--active";
        }

        return classStr;
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
