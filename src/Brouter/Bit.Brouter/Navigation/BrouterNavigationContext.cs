using System.Threading;

namespace Bit.Brouter;

/// <summary>
/// Carries information about an in-progress navigation. Passed to guards and global hooks.
/// Inspired by Vue Router's <c>RouteLocationNormalized</c> and Angular's <c>NavigationStart</c>.
/// </summary>
public sealed class BrouterNavigationContext
{
    private readonly NavigationManager _navManager;

    internal BrouterNavigationContext(BrouterLocation from, BrouterLocation to, CancellationToken cancellationToken, NavigationManager navManager)
    {
        From = from;
        To = to;
        CancellationToken = cancellationToken;
        _navManager = navManager;
    }

    /// <summary>Where the navigation is coming from.</summary>
    public BrouterLocation From { get; }

    /// <summary>The target location.</summary>
    public BrouterLocation To { get; }

    /// <summary>Token cancelled when the navigation is superseded by a newer one.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// How this navigation was initiated - a fresh push, a history-entry replace, or a Back/Forward
    /// traversal. Lets guards, loaders and hooks distinguish a Back navigation from a new push, which
    /// scroll-restoration and analytics logic often needs. Populated before guards run, so it is
    /// available throughout the whole navigation. See <see cref="BrouterNavigationType"/> for the
    /// detection caveats.
    /// </summary>
    public BrouterNavigationType NavigationType { get; internal set; } = BrouterNavigationType.Push;

    /// <summary>
    /// True when this context belongs to a revalidation (<see cref="IBrouter.RevalidateAsync"/>)
    /// rather than a navigation: the URL did not change, guards did not re-run, and only the
    /// matched chain's loaders are executing again. Lets a shared loader distinguish "the user
    /// navigated here" from "the app asked for fresh data after a mutation".
    /// </summary>
    public bool IsRevalidation { get; internal set; }

    /// <summary>
    /// True when this context belongs to a speculative preload (<see cref="IBrouter.PreloadAsync"/> /
    /// <see cref="BrouterLink.Preload"/>) rather than a real navigation: no guards ran, nothing will
    /// render, and the loader result only warms the cache. Loaders with side effects beyond fetching
    /// (analytics, "viewed" markers) should skip them when this is set.
    /// </summary>
    public bool IsPreload { get; internal set; }

    /// <summary>
    /// True when this context belongs to a reload (<see cref="IBrouter.ReloadAsync"/>) rather than a
    /// navigation: the URL did not change, <see cref="IBrouter.OnNavigating"/>/
    /// <see cref="IBrouter.OnNavigated"/> did not fire and no leave guard ran, but - unlike a
    /// revalidation - the matched chain's components were disposed and are being recreated from
    /// scratch, so route (enter) guards and loaders DO run again, exactly as for a chain matched from
    /// nothing. Lets a loader tell "the user navigated here" from "the app threw the current page
    /// away and rebuilt it".
    /// <br/>
    /// Note this covers the navigation EVENTS only: <c>Brouter.OnNavigateAsync</c> - the
    /// lazy-assembly-loading hook that runs before matching - does still fire, because the chain is
    /// being matched from nothing and the assemblies it supplies may be needed to match it.
    /// </summary>
    public bool IsReload { get; internal set; }

    /// <summary>The matched route once matching has happened. Null in OnNavigating hooks.</summary>
    public Broute? Route { get; internal set; }

    /// <summary>Parameters extracted from the matched route. Empty when no match yet.</summary>
    public BrouterRouteParameters Parameters { get; internal set; } = BrouterRouteParameters.Empty;

    /// <summary>True if a guard or hook called <see cref="Cancel"/>.</summary>
    public bool IsCancelled { get; private set; }

    /// <summary>Set when a guard or hook called <see cref="Redirect"/>.</summary>
    public string? RedirectUrl { get; private set; }

    /// <summary>Cancel this navigation. The URL is restored to <see cref="From"/>.
    /// Clears any previously set <see cref="RedirectUrl"/> so the navigation state is unambiguous.</summary>
    public void Cancel()
    {
        IsCancelled = true;
        RedirectUrl = null;
    }

    /// <summary>
    /// Redirect to another URL instead of completing this navigation.
    /// A route-relative <paramref name="url"/> (<c>./x</c>, <c>../x</c>) is resolved against the
    /// path of <see cref="To"/> - the location being navigated to - using segment math, so a guard
    /// on <c>/admin/secret</c> can redirect to <c>"../login"</c> to reach <c>/admin/login</c>.
    /// A redirect to the URL <see cref="To"/> already denotes is "continue": it clears any earlier
    /// redirect and the navigation proceeds. Both URLs are resolved the way
    /// <see cref="NavigationManager.NavigateTo(string, bool)"/> resolves them and compared in full -
    /// any change to the path (casing and trailing slash included), query or hash is a real redirect,
    /// while a different spelling of the same URL (absolute or base-relative, percent-encoding, host
    /// casing, default port, an empty query or hash) is not.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the navigation has already been cancelled
    /// via <see cref="Cancel"/>. A cancelled navigation cannot be turned into a redirect.</exception>
    public void Redirect(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (IsCancelled)
            throw new InvalidOperationException("Cannot set a redirect on a cancelled navigation context. Call Redirect() before Cancel(), or do not cancel.");

        var resolved = BrouterRelativeUrl.ResolveIfRelative(To.Path, url);

        // A redirect to the URL this navigation is already heading to (however it is spelled) is
        // "continue", not a redirect. Honoring it would cancel this navigation and start an
        // identical one, whose guards run again and redirect again - an infinite navigation loop.
        // Treating it as a continue makes guards like "always send anonymous users to /login" safe
        // to write without a "unless we're already going to /login" clause.
        RedirectUrl = IsCurrentTarget(resolved) ? null : resolved;
    }

    /// <summary>
    /// Whether <paramref name="url"/> denotes the URL this navigation is heading to. Both are
    /// resolved by <see cref="NavigationManager.ToAbsoluteUri"/> - as
    /// <see cref="NavigationManager.NavigateTo(string, bool)"/> resolves a redirect - and compared in
    /// canonical form.
    /// </summary>
    /// <remarks>
    /// The comparison is on the URL, not on the route it matches. Only a redirect that would land on
    /// this very URL again can loop, so only that one is a no-op. Any other redirect is honored, even
    /// one the matcher can't tell apart from <see cref="To"/> (a different path casing or trailing
    /// slash under the default options): it still changes the address bar and the casing of route
    /// parameter values, and the navigation it starts heads to the URL that redirect denotes, so
    /// repeating it there is a no-op. <see cref="To"/>'s <see cref="BrouterLocation.FullUri"/> is
    /// re-resolved rather than taken verbatim because it may be the unescaped form
    /// <c>Uri.ToString()</c> produces.
    /// </remarks>
    private bool IsCurrentTarget(string url)
    {
        try
        {
            return string.Equals(BrouterLocation.ToCanonicalUri(_navManager.ToAbsoluteUri(url)),
                                 BrouterLocation.ToCanonicalUri(_navManager.ToAbsoluteUri(To.FullUri)),
                                 StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException or InvalidOperationException)
        {
            // A URL that can't be resolved isn't the current target; navigating to it surfaces the problem.
            return false;
        }
    }
}
