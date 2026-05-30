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

    /// <summary>
    /// Navigate one entry back in history. Fire-and-forget; failures (e.g. JS interop
    /// disconnected) are swallowed. Use <see cref="BackAsync"/> when you need to observe failures.
    /// </summary>
    void Back();

    /// <summary>
    /// Navigate <paramref name="delta"/> entries back in history. Returns a task that
    /// completes when the underlying <c>history.go(-delta)</c> call resolves.
    /// </summary>
    /// <param name="delta">Number of history entries to skip backwards. Must be &gt;= 1.
    /// Defaults to 1 for parity with <see cref="Back"/>.</param>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>. The shipped
    /// <see cref="IBrouter"/> service implements it; implement on custom test doubles if
    /// your code under test exercises history navigation.
    /// </remarks>
    ValueTask BackAsync(int delta = 1) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(BackAsync)}. " +
            "Override the method on your custom implementation to enable history navigation.");

    /// <summary>Navigate one entry forward in history. Fire-and-forget; see <see cref="ForwardAsync"/> for the observable variant.</summary>
    /// <remarks>Default implementation calls <see cref="ForwardAsync"/>. Override either to suit a custom implementation.</remarks>
    void Forward() => _ = ForwardAsync(1);

    /// <summary>Navigate <paramref name="delta"/> entries forward in history.</summary>
    /// <param name="delta">Number of history entries to skip forward. Must be &gt;= 1. Defaults to 1.</param>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>. The shipped
    /// <see cref="IBrouter"/> service implements it; implement on custom test doubles if
    /// your code under test exercises history navigation.
    /// </remarks>
    ValueTask ForwardAsync(int delta = 1) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(ForwardAsync)}. " +
            "Override the method on your custom implementation to enable history navigation.");

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
