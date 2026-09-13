namespace Bit.Brouter;

/// <summary>
/// Payload of <see cref="IBrouterRoute.OnRenavigatingAsync"/>: a pending navigation under which
/// this component's route stays matched - a route/query parameter change, a move between the
/// route's descendants, or a repeated navigation to the same URL. Like
/// <see cref="BrouterRouteDeactivatingContext"/> it runs <em>before</em> the navigation commits
/// and is awaited, so <see cref="BrouterRouteNavigatingContext.Cancel"/> /
/// <see cref="BrouterRouteNavigatingContext.Redirect"/> are preventive - the content simply keeps
/// its current parameters. This closes the classic navigation-lock gap (Vue's
/// <c>beforeRouteUpdate</c>): a route-declared <see cref="Broute.LeaveGuard"/> never fires for a
/// parameter change on its own route, so a dirty edit form on <c>/item/1</c> would be unprotected
/// against navigating to <c>/item/2</c> without this callback.
/// </summary>
public sealed class BrouterRouteRenavigatingContext : BrouterRouteNavigatingContext
{
    internal BrouterRouteRenavigatingContext(BrouterNavigationContext navigation)
        : base(navigation)
    {
    }
}
