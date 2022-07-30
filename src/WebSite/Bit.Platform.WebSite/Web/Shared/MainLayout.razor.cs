using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Platform.WebSite.Web.Shared;

public partial class MainLayout
{
    [Inject]
    public NavigationManager NavigationManager { get; set; }

    public string CurrentUrl { get; set; }

    protected override Task OnInitializedAsync()
    {
        SetCurrentUrl();
        NavigationManager.LocationChanged += OnLocationChanged;

        return base.OnInitializedAsync();
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
