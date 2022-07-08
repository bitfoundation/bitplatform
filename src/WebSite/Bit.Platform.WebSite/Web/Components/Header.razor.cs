﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Platform.WebSite.Web.Components;

public partial class Header : IDisposable
{
    private string CurrentUrl = string.Empty;

    public ElementReference HeaderElement { get; set; }

    [AutoInject] public NavigationManager NavigationManager { get; set; }
    [AutoInject] public IJSRuntime JSRuntime { get; set; }

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.RegisterOnScrollToChangeHeaderStyle(HeaderElement);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
