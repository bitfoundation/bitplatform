using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Bit.Platform.WebSite.Web.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Platform.WebSite.Web.Components;

public partial class GettingStarted
{
    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;
    public string CurrentUrl { get; set; }
    public ElementReference GettingStartedElement { get; set; }

    protected override void OnInitialized()
    {
        CurrentUrl = GetCurrentUrl();

        _navigationManager.LocationChanged += OnLocationChanged;

        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _jsRuntime.RegisterOnScrollToChangeGettingStartedSideRailStyle(GettingStartedElement);
        }

        await base.OnAfterRenderAsync(firstRender);
    }


    private void OnLocationChanged(object sender, LocationChangedEventArgs args)
    {
        CurrentUrl = GetCurrentUrl();

        StateHasChanged();
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }

    private string GetCurrentUrl()
    {
        return _navigationManager.Uri.Replace(_navigationManager.BaseUri + "project-templates/todo-template/getting-started", "", StringComparison.Ordinal);
    }
}
