namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnDeactivatingAsync"/>: a pending navigation that, if it
/// commits, will make this component's routed content stop being the visible route content. Unlike
/// the notify-only lifecycle callbacks, this runs <em>before</em> the navigation commits and is
/// awaited by the pipeline, so <see cref="BrouterRouteNavigatingContext.Cancel"/> /
/// <see cref="BrouterRouteNavigatingContext.Redirect"/> are preventive - the URL never changes
/// when the navigation is blocked. This is the component-level "navigation lock" (React Router's
/// <c>useBlocker</c> / Vue's <c>beforeRouteLeave</c> / Angular's <c>CanDeactivate</c>), the
/// per-content counterpart of the route-declared <see cref="Broute.LeaveGuard"/>.
/// </summary>
public sealed class BrouterRouteDeactivatingContext : BrouterRouteNavigatingContext
{
    internal BrouterRouteDeactivatingContext(BrouterNavigationContext navigation, BrouterRouteDeactivationReason reason)
        : base(navigation)
    {
        Reason = reason;
    }

    /// <summary>
    /// What committing the navigation would do to this content: retained hidden
    /// (<see cref="BrouterRouteDeactivationReason.Hidden"/>, keep-alive - state survives, so a
    /// dirty-form lock may choose not to prompt at all) or unmounted and disposed
    /// (<see cref="BrouterRouteDeactivationReason.Disposing"/>).
    /// </summary>
    public BrouterRouteDeactivationReason Reason { get; }
}
