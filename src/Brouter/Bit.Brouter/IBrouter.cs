using System.Threading.Tasks;

namespace Bit.Brouter;

/// <summary>
/// Service surface for programmatic interaction with the active <see cref="Brouter"/>.
/// Inspired by <c>useNavigate</c>/<c>useLocation</c> in React Router and the <c>$router</c> object in Vue Router.
/// </summary>
public interface IBrouter
{
    /// <summary>The current parsed location. Always non-null; defaults to <see cref="BrouterLocation.Empty"/> before mount.</summary>
    BrouterLocation Location { get; }

    /// <summary>
    /// Imperatively navigate to a URL.
    /// </summary>
    /// <param name="url">Destination URL or path.</param>
    /// <param name="replace">If true, replaces the current history entry instead of pushing a new one.
    /// Ignored when <paramref name="forceLoad"/> is true.</param>
    /// <param name="forceLoad">If true, performs a full-page reload. The Brouter pipeline
    /// (<c>OnNavigating</c>, route guards, loaders, and <c>OnNavigated</c>) is skipped because
    /// the SPA process is replaced by the new document.</param>
    void Navigate(string url, bool replace = false, bool forceLoad = false);

    /// <summary>Navigate one entry back in history.</summary>
    void Back();

    /// <summary>Navigate to a named route, substituting the given parameters into the path.</summary>
    void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                        string? query = null, bool replace = false);

    /// <summary>Build a URL for a named route without navigating.</summary>
    string ResolveUrl(string name, IReadOnlyDictionary<string, object?>? parameters = null, string? query = null);

    /// <summary>Async hook fired before any navigation. Inspect/cancel/redirect via the context.</summary>
    event Func<BrouterNavigationContext, ValueTask>? OnNavigating;

    /// <summary>Async hook fired after a successful navigation completes.</summary>
    event Func<BrouterNavigationContext, ValueTask>? OnNavigated;

    /// <summary>
    /// Async hook fired when an unhandled exception is thrown during navigation
    /// (e.g., from a route loader or another step in the pipeline).
    /// User-driven cancellations via <see cref="BrouterNavigationContext.Cancel"/> and redirects via
    /// <see cref="BrouterNavigationContext.Redirect"/> are control-flow signals and do not raise this event.
    /// </summary>
    event Func<BrouterNavigationContext, Exception?, ValueTask>? OnError;
}
