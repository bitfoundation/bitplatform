using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class MainLayout : IDisposable
{
    private bool _isHomePage;
    private bool _isNavPanelOpen;
    private BitAppShell? _appShellRef;



    [AutoInject] private IExceptionHandler _exceptionHandler = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;
    [AutoInject] private IPrerenderStateService _prerenderStateService = default!;
    [AutoInject] private AppAccentColorService _accentColorService = default!;



    protected override void OnInitialized()
    {
        try
        {
            SetCurrentUrl();
            _navigationManager.LocationChanged += OnLocationChanged;

            base.OnInitialized();
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }

    // The accent is restored from the layout rather than from the home page so a refresh that lands
    // anywhere in the docs comes back with the visitor's color, and so the home page finds it already
    // applied when they navigate back. localStorage is unreachable until the client is live, hence
    // the wait for the first render.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        try
        {
            if (firstRender)
            {
                await _accentColorService.InitializeAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }
        catch (Exception exp)
        {
            _exceptionHandler.Handle(exp);
        }
    }



    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        SetCurrentUrl();
        StateHasChanged();
    }

    private void SetCurrentUrl()
    {
        var url = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);
        _isHomePage = url == "/";
    }



    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
