using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class MainLayout : IDisposable
{
    private bool _isHomePage;

    /// <summary>
    /// Whether the route is one of the ~110 component demo pages. Those are the only pages that
    /// honour prefers-reduced-motion, because on them the animation is the subject rather than
    /// decoration - see the motion policy in Styles/app.scss.
    /// </summary>
    private bool _isDemoPage;
    private bool _isNavPanelOpen;
    private BitAppShell? _appShellRef;



    [AutoInject] private IExceptionHandler _exceptionHandler = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;



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
        _isDemoPage = url.StartsWith("/components/", StringComparison.OrdinalIgnoreCase);
    }



    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
