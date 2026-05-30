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

    /// <summary>The matched route once matching has happened. Null in OnNavigating hooks.</summary>
    public BrouterRoute? Route { get; internal set; }

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

    /// <summary>Redirect to another URL instead of completing this navigation.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the navigation has already been cancelled
    /// via <see cref="Cancel"/>. A cancelled navigation cannot be turned into a redirect.</exception>
    public void Redirect(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        if (IsCancelled)
            throw new InvalidOperationException("Cannot set a redirect on a cancelled navigation context. Call Redirect() before Cancel(), or do not cancel.");
        RedirectUrl = url;
    }
}
