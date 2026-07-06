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
    /// <param name="url">Destination URL or path. A route-relative path (<c>./x</c>, <c>../x</c>)
    /// is resolved against the current location using segment math: from <c>/users/42</c>,
    /// <c>"./edit"</c> navigates to <c>/users/42/edit</c> and <c>"../7"</c> to <c>/users/7</c>.
    /// Bare paths without a leading <c>.</c> keep their base-relative meaning.</param>
    /// <param name="replace">If true, replaces the current history entry instead of pushing a new one.
    /// Ignored when <paramref name="forceLoad"/> is true.</param>
    /// <param name="forceLoad">If true, performs a full-page reload. The Brouter pipeline
    /// (<c>OnNavigating</c>, route guards, loaders, and <c>OnNavigated</c>) is skipped because
    /// the SPA process is replaced by the new document.</param>
    /// <param name="historyState">Optional application state to attach to the destination's history
    /// entry (<c>history.state</c>, via <c>NavigationManager.HistoryEntryState</c>). Read it back on
    /// <see cref="BrouterLocation.HistoryState"/> after the navigation commits - including when the
    /// user later returns to the entry via Back/Forward. Serialize structured data (e.g. JSON)
    /// yourself; the value survives history traversals but not necessarily a full reload.</param>
    void Navigate(string url, bool replace = false, bool forceLoad = false, string? historyState = null);

    /// <summary>
    /// Imperatively navigate to a URL and await how the navigation concluded - committed,
    /// cancelled by a guard, redirected, not found, failed, or superseded by a newer navigation
    /// (see <see cref="BrouterNavigationOutcome"/>). Mirrors Vue Router's awaitable
    /// <c>router.push</c> with navigation failures. URL resolution (including route-relative
    /// <c>./x</c> / <c>../x</c>) matches <see cref="Navigate"/>; full-page reloads
    /// (<c>forceLoad</c>) are not offered here because the SPA process that would resolve the
    /// task is replaced by the new document.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    ValueTask<BrouterNavigationOutcome> NavigateAsync(string url, bool replace = false, string? historyState = null) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(NavigateAsync)}. " +
            "Override the method on your custom implementation to enable awaited navigation.");

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

    /// <summary>
    /// Navigate to a named route, substituting the given parameters into the path.
    /// Parameters that don't match a template parameter are appended as query-string pairs
    /// (after <paramref name="query"/>, if provided). <paramref name="historyState"/> attaches
    /// application state to the destination's history entry (see <see cref="Navigate"/>).
    /// </summary>
    void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                        string? query = null, bool replace = false, string? historyState = null);

    /// <summary>
    /// Build a URL for a named route without navigating. Parameters that don't match a template
    /// parameter are appended as query-string pairs (after <paramref name="query"/>, if provided);
    /// entries with a <see langword="null"/> value are skipped, and non-string enumerable values
    /// emit one pair per element (e.g. <c>?tag=a&amp;tag=b</c>).
    /// </summary>
    string ResolveUrl(string name, IReadOnlyDictionary<string, object?>? parameters = null, string? query = null);

    /// <summary>
    /// Re-runs the loaders of the currently matched route chain and re-renders with the fresh data -
    /// call after a mutation so the screen reflects it (React Router revalidation / SvelteKit
    /// <c>invalidateAll</c> style). Not a navigation: the URL stays, guards and navigation hooks do
    /// not run, and the current content remains visible while loaders work. Loaders can detect it
    /// via <see cref="BrouterNavigationContext.IsRevalidation"/>. Completes when the fresh data has
    /// been committed (or the revalidation was superseded by a navigation).
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    ValueTask RevalidateAsync() =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(RevalidateAsync)}. " +
            "Override the method on your custom implementation to enable revalidation.");

    /// <summary>
    /// Navigates to the current path with a functionally-updated query string: <paramref name="mutate"/>
    /// receives a <see cref="BrouterQueryBuilder"/> seeded with the current query, and every parameter
    /// it doesn't touch is preserved - e.g. <c>brouter.NavigateWithQuery(q =&gt; q.Set("page", 2))</c>
    /// bumps the page while keeping filters/sort intact (TanStack Router's functional search updates).
    /// Replaces the history entry by default, the natural fit for query-as-UI-state; pass
    /// <paramref name="replace"/> = false to push instead. The fragment is preserved.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    void NavigateWithQuery(Action<BrouterQueryBuilder> mutate, bool replace = true) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(NavigateWithQuery)}. " +
            "Override the method on your custom implementation to enable query updates.");

    /// <summary>
    /// Speculatively runs the loaders of the route that <paramref name="url"/> would match, warming
    /// the loader cache so the actual navigation is instant (see also <see cref="BrouterLink.Preload"/>
    /// for declarative hover/viewport preloading). Guards don't run, nothing renders, failures are
    /// swallowed; loaders can detect it via <see cref="BrouterNavigationContext.IsPreload"/>.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    ValueTask PreloadAsync(string url) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(PreloadAsync)}. " +
            "Override the method on your custom implementation to enable preloading.");

    /// <summary>
    /// Drops every cached loader result (see <see cref="Broute.StaleTime"/> and link preloading), so
    /// subsequent navigations re-run their loaders. The blunt companion to
    /// <see cref="RevalidateAsync"/>: call it after a mutation that invalidates data on pages other
    /// than the current one.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    void ClearLoaderCache() =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(ClearLoaderCache)}. " +
            "Override the method on your custom implementation to enable loader-cache invalidation.");

    /// <summary>
    /// Releases every <see cref="Broute.KeepAlive"/> route's retained (hidden) component, leaving
    /// only the currently visible route mounted. The kept components are disposed, so a later return
    /// to one of them recreates it fresh instead of restoring its state. Use it to reclaim memory
    /// held by kept pages - e.g. on sign-out, on a low-memory signal, or after invalidating the state
    /// those pages were holding. A no-op when nothing is being kept.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    void ClearKeepAlive() =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(ClearKeepAlive)}. " +
            "Override the method on your custom implementation to enable keep-alive eviction.");

    /// <summary>
    /// Arms (<c>true</c>) or disarms (<c>false</c>) the browser's generic "leave site?" confirmation
    /// for external navigations - closing the tab, a full reload, or following a link out of the SPA.
    /// Idempotent in both directions, so a dirty-form tracker can toggle it freely. Browser rules
    /// apply (dialog requires prior user interaction; text not customizable). In-SPA navigations are
    /// covered by <see cref="Broute.LeaveGuard"/> / <see cref="OnNavigating"/> instead. See also
    /// <see cref="BrouterOptions.ConfirmExternalNavigation"/> for the always-on variant.
    /// </summary>
    /// <remarks>
    /// Default implementation throws <see cref="NotSupportedException"/>; the shipped
    /// <see cref="IBrouter"/> service implements it. Override on custom test doubles if needed.
    /// </remarks>
    ValueTask SetConfirmExternalNavigationAsync(bool enabled) =>
        throw new NotSupportedException(
            $"This {nameof(IBrouter)} implementation does not support {nameof(SetConfirmExternalNavigationAsync)}. " +
            "Override the method on your custom implementation to enable external-navigation confirmation.");

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
