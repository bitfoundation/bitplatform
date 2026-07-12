using System.Threading;

namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnRenavigatingAsync"/>: a pending navigation under which
/// this component's route stays matched - a route/query parameter change, a move between the
/// route's descendants, or a repeated navigation to the same URL. Like
/// <see cref="BrouterRouteDeactivatingContext"/> it runs <em>before</em> the navigation commits
/// and is awaited, so <see cref="Cancel"/> / <see cref="Redirect"/> are preventive. This closes
/// the classic navigation-lock gap (Vue's <c>beforeRouteUpdate</c>): a route-declared
/// <see cref="Broute.LeaveGuard"/> never fires for a parameter change on its own route, so a dirty
/// edit form on <c>/item/1</c> would be unprotected against navigating to <c>/item/2</c> without
/// this callback.
/// </summary>
public sealed class BrouterRouteRenavigatingContext
{
    private readonly BrouterNavigationContext _navigation;

    internal BrouterRouteRenavigatingContext(BrouterNavigationContext navigation)
    {
        _navigation = navigation;
    }

    /// <summary>Where the pending navigation is coming from (where the user is now).</summary>
    public BrouterLocation From => _navigation.From;

    /// <summary>The pending target location.</summary>
    public BrouterLocation To => _navigation.To;

    /// <summary>How the pending navigation was initiated (push / replace / Back-Forward pop).</summary>
    public BrouterNavigationType NavigationType => _navigation.NavigationType;

    /// <summary>
    /// Cancelled when the pending navigation is superseded by a newer one. A callback awaiting user
    /// input (a custom confirmation dialog) should observe it and dismiss the prompt - the decision
    /// no longer matters.
    /// </summary>
    public CancellationToken CancellationToken => _navigation.CancellationToken;

    /// <summary>True once <see cref="Cancel"/> has been called (by this or an earlier handler).</summary>
    public bool IsCancelled => _navigation.IsCancelled;

    /// <summary>
    /// Block the pending navigation. Preventive: the URL never changes, history stays intact, and
    /// the content simply remains on screen with its current parameters.
    /// </summary>
    public void Cancel() => _navigation.Cancel();

    /// <summary>
    /// Send the navigation somewhere else instead (same semantics as
    /// <see cref="BrouterNavigationContext.Redirect"/>, including route-relative resolution
    /// against <see cref="To"/>).
    /// </summary>
    public void Redirect(string url) => _navigation.Redirect(url);

    // See BrouterRouteDeactivatingContext.HasDecision.
    internal bool HasDecision => _navigation.IsCancelled || _navigation.RedirectUrl is not null;
}
