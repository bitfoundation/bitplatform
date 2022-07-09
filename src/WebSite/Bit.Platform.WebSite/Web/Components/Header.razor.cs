using System;
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

    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IJSRuntime _jsRuntime = default!;

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _jsRuntime.RegisterOnScrollToChangeHeaderStyle(HeaderElement);
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
