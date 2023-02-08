using System;
using Bit.BlazorUI.Playground.Web.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Playground.Web.Shared;

public partial class MainLayout : IDisposable
{
    [Inject] public NavigationManager NavigationManager { get; set; }

    [Inject] private IExceptionHandler exceptionHandler { get; set; } = default!;

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
