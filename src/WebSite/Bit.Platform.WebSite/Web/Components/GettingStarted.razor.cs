using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Bit.Platform.WebSite.Web.Shared;

namespace Bit.Platform.WebSite.Web.Components;

public partial class GettingStarted
{
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public IJSRuntime JSRuntime { get; set; }
    public string CurrentUrl { get; set; }
    public ElementReference GettingStartedElement { get; set; }

    protected override void OnInitialized()
    {
        CurrentUrl = NavigationManager.Uri.Replace(GetCurrentUrl(), "", StringComparison.Ordinal);

        NavigationManager.LocationChanged += OnLocationChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.RegisterOnScrollToChangeGettingStartedSideRailStyle(GettingStartedElement);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(GetCurrentUrl(), "", StringComparison.Ordinal);
        StateHasChanged();
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }

    private string GetCurrentUrl()
    {
        return NavigationManager.BaseUri + "project-templates/todo-template/getting-started";
    }
}
