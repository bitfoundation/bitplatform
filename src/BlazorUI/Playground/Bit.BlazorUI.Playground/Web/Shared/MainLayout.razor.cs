using System;
using Bit.BlazorUI.Playground.Web.Services.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BlazorUI.Playground.Web.Shared;

public partial class MainLayout : IDisposable
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
            CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
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
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
