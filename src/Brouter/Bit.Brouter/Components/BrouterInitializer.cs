using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Brouter;

/// <summary>
/// The boot sentinel <see cref="Brouter"/> emits as the last frame of its child region, responsible
/// for firing the initial navigation at the right moment - and, crucially, in the right render batch.
/// </summary>
/// <remarks>
/// <para>
/// The initial route match cannot run until the declared &lt;Broute&gt; children have registered,
/// and registration happens as the declaration tree renders: each nesting level (every Broute, every
/// CascadingValue between it and its children, every user component wrapped around routes) initializes
/// one render-queue "wave" after its parent rendered. Brouter used to bridge that with
/// <c>await Task.Yield()</c> in <c>OnInitializedAsync</c> - correct, but it forced the mount into TWO
/// render batches: batch one rendered the (empty) unmatched router, batch two revealed the page. After
/// prerendering that empty first interactive batch replaced the server-rendered HTML with nothing for
/// a frame - a visible flash of blank content (prerendered page -&gt; blank -&gt; page).
/// </para>
/// <para>
/// The sentinel keeps everything inside the FIRST batch instead, exploiting two renderer facts:
/// a component created during a render initializes synchronously within the same render-queue drain,
/// and its own first render is appended to the tail of that same queue. So a sentinel that re-emits
/// itself as a child observes the route tree one wave later per generation - without ever leaving the
/// batch. Each generation compares <see cref="Brouter.RouteRegistrationVersion"/> against what the
/// previous generation saw: a change means declarations are still registering (keep waiting); once
/// the version has stayed quiet for <see cref="RequiredQuietRounds"/> consecutive generations the
/// tree is taken as settled and a <see cref="BrouterInitialNavigator"/> is emitted, whose
/// OnInitializedAsync runs the initial navigation. A fully synchronous pipeline (no loaders) then
/// commits the matched route into the same batch - the first frame the user sees already contains
/// the page, and the interactive takeover after prerender is seamless.
/// </para>
/// <para>
/// The quiet-round threshold is a heuristic for one reason: opaque wrapper components. A chain of
/// user components between the router and its routes registers nothing for one wave per wrapper, and
/// no library code can observe "the render queue still holds work that will register routes". The
/// per-level cadence of the route tree itself (Broute render -&gt; CascadingValue render) goes quiet
/// for at most one wave per level, so <see cref="RequiredQuietRounds"/> = 3 settles correctly for
/// arbitrarily deep route nesting and up to three consecutive non-registering wrapper renders.
/// Deeper wrapper stacks make the navigator fire before every route registered - which is exactly
/// the late-registration case <see cref="Brouter.RegisterRoute"/>'s rematch handles: the outcome is
/// corrected in a follow-up batch (no worse than the old two-batch mount), never silently wrong.
/// </para>
/// <para>
/// Once <see cref="Brouter.InitialNavigationStarted"/> is true every sentinel renders empty, so the
/// chain unwinds (children dispose) on its next render and steady-state cost is a single inert
/// component. Static prerendering works unchanged: all of this runs in component lifecycle the
/// prerenderer awaits, and a pending navigator task (loaders) is tracked for quiescence exactly
/// like the old OnInitializedAsync was.
/// </para>
/// </remarks>
internal sealed class BrouterInitializer : ComponentBase
{
    /// <summary>
    /// Consecutive registration-quiet generations required before the route tree is considered
    /// settled. See the class remarks for why 3: route nesting itself never goes quiet for more
    /// than one consecutive wave, so this tolerates up to three stacked non-registering wrapper
    /// components while costing only two extra (trivially cheap) sentinel renders per mount.
    /// </summary>
    internal const int RequiredQuietRounds = 3;

    /// <summary>
    /// Backstop against a pathological route tree that never stops registering (which would
    /// otherwise grow the sentinel chain forever inside one batch). At the cap the navigator
    /// fires regardless; the late-registration rematch corrects the outcome if needed.
    /// </summary>
    internal const int MaxGenerations = 256;

    [Parameter] public Brouter Router { get; set; } = default!;

    /// <summary>
    /// The <see cref="Brouter.RouteRegistrationVersion"/> observed when this generation was emitted.
    /// Parameter values are baked at frame-build time, i.e. BEFORE the same render's sibling Broutes
    /// initialize - the first generation therefore always observes a change, which is fine: it just
    /// spends one round.
    /// </summary>
    [Parameter] public long ObservedVersion { get; set; }

    /// <summary>Consecutive quiet observations accumulated so far (reset to 0 by any change).</summary>
    [Parameter] public int QuietRounds { get; set; }

    /// <summary>This node's position in the sentinel chain, for the runaway cap.</summary>
    [Parameter] public int Generation { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        base.BuildRenderTree(builder);

        // Inert once boot is done. Rendering empty here is also what unwinds the chain: the next
        // render of each node diffs its child sentinel/navigator away.
        if (Router.InitialNavigationStarted) return;

        var current = Router.RouteRegistrationVersion;

        if (Generation >= MaxGenerations)
        {
            RenderNavigator(builder);
            return;
        }

        if (current != ObservedVersion)
        {
            // Declarations registered since the previous generation looked - keep observing.
            RenderChildSentinel(builder, current, quietRounds: 0);
            return;
        }

        var quiet = QuietRounds + 1;
        if (quiet < RequiredQuietRounds)
        {
            RenderChildSentinel(builder, current, quiet);
            return;
        }

        RenderNavigator(builder);
    }

    private void RenderChildSentinel(RenderTreeBuilder builder, long version, int quietRounds)
    {
        builder.OpenComponent<BrouterInitializer>(0);
        builder.AddAttribute(1, nameof(Router), Router);
        builder.AddAttribute(2, nameof(ObservedVersion), version);
        builder.AddAttribute(3, nameof(QuietRounds), quietRounds);
        builder.AddAttribute(4, nameof(Generation), Generation + 1);
        builder.CloseComponent();
    }

    // Distinct sequence numbers from the sentinel branch so a branch switch diffs as a clean
    // remove+insert rather than an in-place parameter morph of unrelated components.
    private void RenderNavigator(RenderTreeBuilder builder)
    {
        builder.OpenComponent<BrouterInitialNavigator>(5);
        builder.AddAttribute(6, nameof(BrouterInitialNavigator.Router), Router);
        builder.CloseComponent();
    }
}

/// <summary>
/// Terminal node of the <see cref="BrouterInitializer"/> chain: runs the initial navigation from
/// its OnInitializedAsync, which the renderer invokes synchronously while diffing the emitting
/// sentinel - still inside the first render batch. A navigation with no async work (no guards or
/// loaders awaiting) therefore commits its matched route into that same batch. When the pipeline
/// does await, the incomplete lifecycle task is tracked by the renderer exactly as
/// <c>Brouter.OnInitializedAsync</c>'s used to be: static prerendering waits for it before
/// serializing HTML, and a <c>NavigationException</c> (the framework's SSR redirect signal, e.g.
/// an auth gate's loader redirecting) propagates out of it so the endpoint can issue the HTTP
/// redirect.
/// </summary>
internal sealed class BrouterInitialNavigator : ComponentBase
{
    [Parameter] public Brouter Router { get; set; } = default!;

    protected override Task OnInitializedAsync() => Router.RunInitialNavigationAsync();
}
