﻿using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Bit.BlazorUI.Demo.Web.Services.Contracts;

namespace Bit.BlazorUI.Demo.Web.Shared;

public partial class Footer
{
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    [Inject]
    private IExceptionHandler exceptionHandler { get; set; } = default!;

    public string CurrentUrl { get; set; }

    protected override void OnInitialized()
    {
        try
        {
            SetCurrentUrl();
            NavigationManager.LocationChanged += OnLocationChanged;

            base.OnInitialized();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
