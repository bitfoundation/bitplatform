using System.Threading;

namespace Bit.Brouter;

/// <summary>
/// Shared payload of the pre-commit navigation-lock callbacks
/// (<see cref="IBrouterRoute.OnDeactivatingAsync"/> /
/// <see cref="IBrouterRoute.OnRenavigatingAsync"/>): the pending navigation's state plus the
/// preventive <see cref="Cancel"/> / <see cref="Redirect"/> decisions. Handlers run
/// <em>before</em> the navigation commits and are awaited by the pipeline, so a decision here
/// means the URL never changes and history stays intact.
/// </summary>
public abstract class BrouterRouteNavigatingContext
{
    private readonly BrouterNavigationContext _navigation;

    private protected BrouterRouteNavigatingContext(BrouterNavigationContext navigation) => _navigation = navigation;

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
