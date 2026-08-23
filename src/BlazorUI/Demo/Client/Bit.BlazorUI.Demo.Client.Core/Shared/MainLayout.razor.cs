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
        // Only when one of the two flags actually moved. Rendering the layout means rendering the nav
        // panel with every component in the library in it, plus the header and the footer - and the
        // answer to "is this the home page" and "is this a component page" is the same for every one
        // of the hundred navigations from one component page to the next, which is exactly the
        // navigation that has to be quick.
        if (SetCurrentUrl() is false) return;

        StateHasChanged();
    }

    /// <summary>Returns whether either flag changed.</summary>
    private bool SetCurrentUrl()
    {
        var url = _navigationManager.Uri.Replace(_navigationManager.BaseUri, "/", StringComparison.InvariantCultureIgnoreCase);

        var isHomePage = url == "/";
        var isDemoPage = url.StartsWith("/components/", StringComparison.OrdinalIgnoreCase);

        if (isHomePage == _isHomePage && isDemoPage == _isDemoPage) return false;

        _isHomePage = isHomePage;
        _isDemoPage = isDemoPage;

        return true;
    }



    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
    }
}
