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

    /// <summary>
    /// The address of the page currently on screen, without its fragment, which is what tells a
    /// navigation to another page apart from a link into this one.
    /// </summary>
    private string _currentPath = string.Empty;



    [AutoInject] private IExceptionHandler _exceptionHandler = default!;
    [AutoInject] private NavigationManager _navigationManager = default!;



    protected override void OnInitialized()
    {
        try
        {
            SetCurrentUrl();
            _currentPath = GetPath(_navigationManager.Uri);
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
        GoToTopOnPageChange(args.Location);

        // Only when one of the two flags actually moved. Rendering the layout means rendering the nav
        // panel with every component in the library in it, plus the header and the footer - and the
        // answer to "is this the home page" and "is this a component page" is the same for every one
        // of the hundred navigations from one component page to the next, which is exactly the
        // navigation that has to be quick.
        if (SetCurrentUrl() is false) return;

        StateHasChanged();
    }

    /// <summary>
    /// Puts a new page on screen at its top. The scroller is the app shell's main element, and it is
    /// the same element from one page to the next, so nothing resets it on its own: without this the
    /// reader lands in a new page at wherever the previous one left them.
    /// <para>
    /// On a component demo page that is worse than merely disorienting. Such a page holds back what
    /// the reader has not reached and mounts it as they approach it, and everything ABOVE the scroll
    /// position counts as reached - so landing halfway down one fills those examples in a render at a
    /// time, each of them growing the page above the reader and sliding the rest down under their
    /// eyes. Starting at the top means there is nothing above them to fill in.
    /// </para>
    /// <para>
    /// A change of fragment alone is left alone - that is a link into the page already on screen, and
    /// the API tables' #component-dir style links are exactly that. So is a navigation that carries a
    /// fragment of its own, which brings its own destination for the browser to scroll to.
    /// </para>
    /// </summary>
    private void GoToTopOnPageChange(string location)
    {
        var path = GetPath(location);

        var isSamePage = string.Equals(path, _currentPath, StringComparison.OrdinalIgnoreCase);

        _currentPath = path;

        if (isSamePage) return;
        if (location.Contains('#', StringComparison.Ordinal)) return;

        _ = GoToTopAsync();
    }

    private async Task GoToTopAsync()
    {
        try
        {
            if (_appShellRef is null) return;

            await _appShellRef.GoToTop(BitScrollBehavior.Instant);
        }
        catch (JSDisconnectedException) { } // the circuit is already gone, and with it the page being scrolled
        catch (Exception exp)
        {
            // Fire-and-forget, so a rethrow here would be an unobserved task and nobody would hear it.
            _exceptionHandler.Handle(exp);
        }
    }

    private static string GetPath(string url)
    {
        var index = url.IndexOf('#', StringComparison.Ordinal);

        return index < 0 ? url : url[..index];
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
