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
