using System.Threading;

namespace Bit.Brouter;

/// <summary>
/// Carries information about an in-progress navigation. Passed to guards and global hooks.
/// Inspired by Vue Router's <c>RouteLocationNormalized</c> and Angular's <c>NavigationStart</c>.
/// </summary>
public sealed class BrouterNavigationContext
{
    internal BrouterNavigationContext(BrouterLocation from, BrouterLocation to, CancellationToken cancellationToken)
    {
        From = from;
        To = to;
        CancellationToken = cancellationToken;
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

        // A redirect to the exact location this navigation is already heading to is "continue",
        // not a redirect. Honoring it would cancel this navigation and start an identical one,
        // whose guards run again and redirect again - an infinite navigation loop. Treating it
        // as a no-op makes guards like "always send anonymous users to /login" safe to write
        // without a "unless we're already going to /login" clause.
        if (IsCurrentTarget(resolved)) return;

        RedirectUrl = resolved;
    }

    /// <summary>
    /// Whether <paramref name="resolved"/> denotes the same location this navigation is heading to.
    /// Compared against both the absolute URI and the base-relative path+query+hash forms, since a
    /// redirect may be expressed either way.
    /// </summary>
    private bool IsCurrentTarget(string resolved)
    {
        if (string.Equals(resolved, To.FullUri, StringComparison.OrdinalIgnoreCase)) return true;

        var pathQueryHash = To.Path + To.Query + To.Hash;
        if (string.Equals(resolved, pathQueryHash, StringComparison.OrdinalIgnoreCase)) return true;

        // Base-relative form without the leading slash ("users/1" vs "/users/1").
        return resolved.Length > 0 && resolved[0] != '/' &&
               string.Equals("/" + resolved, pathQueryHash, StringComparison.OrdinalIgnoreCase);
    }
}
