using System.Threading;

namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnDeactivatingAsync"/>: a pending navigation that, if it
/// commits, will make this component's routed content stop being the visible route content. Unlike
/// the notify-only lifecycle callbacks, this runs <em>before</em> the navigation commits and is
/// awaited by the pipeline, so <see cref="Cancel"/> / <see cref="Redirect"/> are preventive - the
/// URL never changes when the navigation is blocked. This is the component-level "navigation lock"
/// (React Router's <c>useBlocker</c> / Vue's <c>beforeRouteLeave</c> / Angular's
/// <c>CanDeactivate</c>), the per-content counterpart of the route-declared
/// <see cref="Broute.LeaveGuard"/>.
/// </summary>
public sealed class BrouterRouteDeactivatingContext
{
    private readonly BrouterNavigationContext _navigation;

    internal BrouterRouteDeactivatingContext(BrouterNavigationContext navigation, BrouterRouteDeactivationReason reason)
    {
        _navigation = navigation;
        Reason = reason;
    }

    /// <summary>Where the pending navigation is coming from (where the user is now).</summary>
    public BrouterLocation From => _navigation.From;

    /// <summary>
    /// The pending target location - show it in a custom "unsaved changes" prompt so the user knows
    /// where they were headed.
    /// </summary>
    public BrouterLocation To => _navigation.To;

    /// <summary>How the pending navigation was initiated (push / replace / Back-Forward pop).</summary>
    public BrouterNavigationType NavigationType => _navigation.NavigationType;

    /// <summary>
    /// What committing the navigation would do to this content: retained hidden
    /// (<see cref="BrouterRouteDeactivationReason.Hidden"/>, keep-alive - state survives, so a
    /// dirty-form lock may choose not to prompt at all) or unmounted and disposed
    /// (<see cref="BrouterRouteDeactivationReason.Disposing"/>).
    /// </summary>
    public BrouterRouteDeactivationReason Reason { get; }

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
    /// the content simply remains on screen.
    /// </summary>
    public void Cancel() => _navigation.Cancel();

    /// <summary>
    /// Send the navigation somewhere else instead (same semantics as
    /// <see cref="BrouterNavigationContext.Redirect"/>, including route-relative resolution
    /// against <see cref="To"/>).
    /// </summary>
    public void Redirect(string url) => _navigation.Redirect(url);

    // Whether a handler already decided this navigation's fate (cancel or redirect) - the dispatch
    // loop stops at the first decision so later handlers never see an already-settled navigation.
    internal bool HasDecision => _navigation.IsCancelled || _navigation.RedirectUrl is not null;
}
