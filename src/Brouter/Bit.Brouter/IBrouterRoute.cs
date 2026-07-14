namespace Bit.Brouter;

/// <summary>
/// Opt-in route lifecycle for components rendered by a <see cref="Broute"/> - every route, not just
/// keep-alive ones. Implement it (all members have no-op defaults, so override only what you need)
/// and the router calls back when the component's routed content:
/// <list type="bullet">
/// <item><see cref="OnActivatedAsync"/> - became the committed, visible route content: on the first
/// show (with <see cref="BrouterRouteActivation.IsFirstActivation"/> set) and again every time a
/// kept-alive instance is revealed from its hidden retention.</item>
/// <item><see cref="OnDeactivatedAsync"/> - stopped being the visible content: hidden but retained
/// (<see cref="BrouterRouteDeactivationReason.Hidden"/>, keep-alive) or about to be unmounted and
/// disposed (<see cref="BrouterRouteDeactivationReason.Disposing"/>). <see cref="Broute.KeepAlive"/>
/// only changes which of the two follows - the lifecycle itself is universal, so a page written
/// against it keeps working when the route's retention policy changes.</item>
/// <item><see cref="OnRenavigatedAsync"/> - stayed the visible content while a navigation
/// re-committed it with the <em>same instance</em> (route/query parameter changes on a singleton
/// route). This is the discrete "the user arrived here again" moment that <c>OnInitialized</c>
/// famously misses when Blazor reuses the component. On a per-parameter keep-alive route
/// (<see cref="Broute.KeepAliveMax"/> &gt; 1) a parameter change mounts a separate instance instead,
/// so it surfaces as an activate/deactivate pair rather than a renavigation.</item>
/// <item><see cref="OnDeactivatingAsync"/> / <see cref="OnRenavigatingAsync"/> - the pre-commit,
/// cancellable counterparts (the component-level navigation lock): called BEFORE a pending
/// navigation commits, awaited by the pipeline, able to cancel or redirect it preventively. See
/// each member for details.</item>
/// </list>
/// The Ionic <c>ionViewWillEnter</c>/<c>ionViewDidLeave</c> and Vue <c>onActivated</c>/<c>onDeactivated</c>
/// idea, delivered to the component itself.
/// </summary>
/// <remarks>
/// <para><b>How the router finds the implementation.</b> A component instantiated directly by the
/// router (a <see cref="Broute.Component"/> route, including attribute-discovered <c>@page</c>
/// routes) is discovered automatically - implementing the interface is all it takes. Content
/// rendered as a fragment (<see cref="Broute.Content"/>) or through a <see cref="Brouter.Found"/>
/// template (where the framework's <c>RouteView</c> instantiates the page) is not visible to the
/// router; there, derive from <see cref="BrouterRouteBase"/> or register by hand via the cascaded
/// <see cref="BrouterRouteContext"/>. Registration also works for any <em>descendant</em> component
/// of the routed content, at any depth - not just the page root.</para>
/// <para><b>Timing and ordering.</b> Activation and renavigation callbacks run after the
/// navigation's commit render has been applied, root -> leaf (parents before children, like guards
/// and loaders), so the DOM is available - and are therefore never invoked during static
/// prerendering, where no interactive render completes (<see cref="BrouterRouteContext.IsActive"/>
/// still reads correctly there). Deactivation runs as part of the navigation pipeline before the
/// render that hides or unmounts the content, leaf -> root (children flush state before their
/// parents tear shared context down, mirroring leave guards) - so for
/// <see cref="BrouterRouteDeactivationReason.Disposing"/> the synchronous part of the callback is
/// guaranteed to run before <c>Dispose</c>. Content destroyed <em>outside</em> a navigation (a
/// conditionally-removed route, a hosting layout or outlet unmounting) gets a best-effort
/// Disposing deactivation as its owner tears down.</para>
/// <para><b>Async.</b> The notification callbacks (activated / deactivated / renavigated) return
/// tasks that are observed for errors (surfaced via <see cref="IBrouter.OnError"/>) but never
/// block or delay the navigation. The lock callbacks (<see cref="OnDeactivatingAsync"/> /
/// <see cref="OnRenavigatingAsync"/>) are the deliberate exception: the pipeline awaits them -
/// delaying the pending navigation is their purpose - handlers run sequentially leaf -&gt; root,
/// the first cancel/redirect wins and skips the rest, and a callback that throws blocks the
/// navigation (fail closed, like a guard) with the error surfaced via <see cref="IBrouter.OnError"/>.
/// Locks only run for interactive navigations the router can intercept - never during static
/// prerendering, and not for external departures (tab close, reload, external links), for which
/// <see cref="BrouterOptions.ConfirmExternalNavigation"/> /
/// <see cref="IBrouter.SetConfirmExternalNavigationAsync"/> arm the browser's generic dialog.
/// Permanent teardown
/// still surfaces through normal component disposal (<c>IDisposable</c>): retained instances
/// dropped by LRU eviction or <see cref="IBrouter.ClearKeepAlive"/> were already deactivated
/// (<c>Hidden</c>) when they were hidden, so disposal is their only remaining signal - a kept
/// instance whose hosting subtree unmounts is likewise disposed after its earlier Hidden
/// deactivation.</para>
/// </remarks>
public interface IBrouterRoute
{
    /// <summary>
    /// Called after this component's routed content became the committed, visible route content -
    /// on the initial show and again every time a kept-alive instance is revealed. Runs after the
    /// commit render has been applied (DOM available). Use it for "whenever the user lands here"
    /// work: resume timers/subscriptions, refresh stale data on return.
    /// </summary>
    ValueTask OnActivatedAsync(BrouterRouteActivation activation) => ValueTask.CompletedTask;

    /// <summary>
    /// Called when this component's routed content stops being the visible route content - hidden
    /// but retained (keep-alive) or about to be unmounted and disposed, per
    /// <see cref="BrouterRouteDeactivation.Reason"/>. Runs before the render that hides/unmounts
    /// the content. Use it to pause background work or flush state.
    /// </summary>
    ValueTask OnDeactivatedAsync(BrouterRouteDeactivation deactivation) => ValueTask.CompletedTask;

    /// <summary>
    /// Called after a navigation re-committed this route while the same component instance stayed
    /// visible (parameter/query changes on a singleton route, or a repeated navigation to the same
    /// route). Runs after the commit render, like activation. Use it for "the user arrived here
    /// again" work that <c>OnInitialized</c> misses on instance reuse.
    /// </summary>
    ValueTask OnRenavigatedAsync(BrouterRouteRenavigation renavigation) => ValueTask.CompletedTask;

    /// <summary>
    /// Called BEFORE a pending navigation that would deactivate this component's routed content
    /// commits - the component-level navigation lock. Unlike the notify-only callbacks above, the
    /// pipeline awaits it, and <see cref="BrouterRouteNavigatingContext.Cancel"/> /
    /// <see cref="BrouterRouteNavigatingContext.Redirect"/> are preventive: the URL never
    /// changes when the navigation is blocked. The callback may await user input (a custom
    /// "unsaved changes" dialog) - render the prompt, await its completion, then decide - while
    /// observing <see cref="BrouterRouteNavigatingContext.CancellationToken"/> so a superseding
    /// navigation dismisses the prompt. See <see cref="OnRenavigatingAsync"/> for navigations that
    /// keep this route matched (parameter changes).
    /// </summary>
    ValueTask OnDeactivatingAsync(BrouterRouteDeactivatingContext context) => ValueTask.CompletedTask;

    /// <summary>
    /// Called BEFORE a pending navigation under which this component's route stays matched commits
    /// (a parameter/query change or a move between the route's descendants) - the renavigation
    /// half of the navigation lock, needed because a parameter change is not a "leave" (a dirty
    /// form on <c>/item/1</c> must also be able to veto going to <c>/item/2</c>). Same awaited,
    /// preventive semantics as <see cref="OnDeactivatingAsync"/>.
    /// </summary>
    ValueTask OnRenavigatingAsync(BrouterRouteRenavigatingContext context) => ValueTask.CompletedTask;
}
