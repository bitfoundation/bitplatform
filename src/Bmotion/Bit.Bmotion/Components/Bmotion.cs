using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bmotion;
/// <summary>
/// The primary animation component. It renders no element of its own: the consumer authors the
/// animated element inside <see cref="ChildContent"/> and Bmotion automatically injects the
/// engine wiring (element id, the initial pre-first-paint inline style, and <c>pathLength</c>
/// for path drawing) into the first root HTML element by rewriting the child content's
/// render-tree frames - no attribute splatting required.
/// Animation math runs in the C# <see cref="BmotionAnimationEngine"/>; JS is used only
/// for DOM style mutation, pointer/focus events, viewport observation and FLIP.
/// </summary>
public sealed class Bmotion : ComponentBase, IAsyncDisposable
{
    // ── Injected services ──────────────────────────────────────────────────────
    [Inject] private BmotionAnimationEngine Engine { get; set; } = null!;
    [Inject] private IBmotionInterop Interop { get; set; } = null!;
    [Inject] private BmotionLayoutRegistry LayoutRegistry { get; set; } = null!;
    [Inject] private BitBmotionOptions Options { get; set; } = null!;
    [Inject] private ILogger<Bmotion>? Logger { get; set; }

    // ── Cascaded contexts ──────────────────────────────────────────────────────
    [CascadingParameter] private BmotionPresenceContext? PresenceCtx { get; set; }
    [CascadingParameter] private BmotionVariantContext? VariantCtx { get; set; }
    [CascadingParameter] private BmotionConfigContext? ConfigCtx { get; set; }
    [CascadingParameter] private BmotionLayoutGroupContext? LayoutGroupCtx { get; set; }

    // ── Core rendering parameters ────────────────────────────────────────────
    /// <summary>
    /// Optional stable id for the animated element (otherwise a unique one is generated, or an
    /// <c>id</c> authored on the element itself is adopted) so external CSS/JS and programmatic
    /// APIs can target it by selector. The id is the element's engine identity, so changing it
    /// after first render is unsupported.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// The child content that authors the animated element. The first root HTML element receives
    /// the engine wiring automatically (id, initial style - merged before any <c>style</c> you
    /// author, so yours wins conflicts - and <c>pathLength</c>); additional root nodes render
    /// unchanged. The root being animated must be a plain element, not a component.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Animation targets ──────────────────────────────────────────────────────
    [Parameter] public BmTarget? Initial { get; set; }
    [Parameter] public BmTarget? Animate { get; set; }
    [Parameter] public BmTarget? Exit { get; set; }

    // ── Gesture states ─────────────────────────────────────────────────────────
    [Parameter] public BmTarget? WhileHover { get; set; }
    [Parameter] public BmTarget? WhileTap { get; set; }
    [Parameter] public BmTarget? WhileFocus { get; set; }
    [Parameter] public BmTarget? WhileDrag { get; set; }
    [Parameter] public BmTarget? WhileInView { get; set; }

    /// <summary>
    /// If <c>true</c>, <see cref="WhileInView"/> fires only once and never deactivates.
    /// Shorthand for <c>Viewport = new BmViewport { Once = true }</c>.
    /// </summary>
    [Parameter] public bool Once { get; set; }

    /// <summary>
    /// Advanced viewport options for <see cref="WhileInView"/> (margin, amount, once).
    /// When set, <see cref="Once"/> is ignored in favour of <c>Viewport.Once</c>.
    /// </summary>
    [Parameter] public BmViewport? Viewport { get; set; }

    // ── Transition ─────────────────────────────────────────────────────────────
    [Parameter] public BmTransition? Transition { get; set; }

    // ── Variants ─────────────────────────────────────────────────────────────
    [Parameter] public BmVariants? Variants { get; set; }

    /// <summary>
    /// Active variant name - the razor-literal-friendly way to drive variants:
    /// <c>State="@(_open ? "open" : "closed")"</c>. Equivalent to a variant-name
    /// <see cref="Animate"/> target (<see cref="Animate"/> wins when both are set) and
    /// propagates to descendant Bmotion components like any variant label.
    /// </summary>
    [Parameter] public string? State { get; set; }

    /// <summary>
    /// Initial variant name (razor-literal friendly). <see cref="Initial"/> wins when both are set.
    /// </summary>
    [Parameter] public string? InitialState { get; set; }

    /// <summary>
    /// Per-component data passed to dynamic variants (entries added via
    /// <see cref="BmVariants.Add(string, Func{object?, BmProps})"/>).
    /// </summary>
    [Parameter] public object? Custom { get; set; }

    // ── Motion-value bindings ──────────────────────────────────────────────────
    /// <summary>
    /// Binds motion values to engine properties ("x", "opacity", "rotate", …) - the equivalent of
    /// motion.dev's <c>style={{ x }}</c>. Every value change is flushed straight to the element
    /// on the next frame without re-rendering the component:
    /// <code>
    /// &lt;Bmotion Values='new() { ["x"] = _x, ["opacity"] = _fade }'&gt;
    ///     &lt;div class="box" /&gt;
    /// &lt;/Bmotion&gt;
    /// </code>
    /// </summary>
    [Parameter] public Dictionary<string, BmValue<double>>? Values { get; set; }

    /// <summary>
    /// Binds string motion values - typically <see cref="Bm.Template"/> composites - to CSS
    /// properties ("filter", "clipPath", "boxShadow", …), the equivalent of motion.dev's
    /// <c>useMotionTemplate</c>. Every change is flushed straight to the element on the next
    /// frame without re-rendering the component:
    /// <code>
    /// &lt;Bmotion StringValues='new() { ["filter"] = _filter }'&gt;
    ///     &lt;div class="box" /&gt;
    /// &lt;/Bmotion&gt;
    /// </code>
    /// </summary>
    [Parameter] public Dictionary<string, BmValue<string>>? StringValues { get; set; }

    // ── Drag ─────────────────────────────────────────────────────────────────
    /// <summary>Enable dragging: <c>Drag="true"</c> (both axes), <c>Drag="BmDrag.X"</c> or <c>Drag="BmDrag.Y"</c>.</summary>
    [Parameter] public BmDrag Drag { get; set; }

    /// <summary>Constraint bounds in px relative to the element's resting position.</summary>
    [Parameter] public BmDragConstraints? DragConstraints { get; set; }

    /// <summary>
    /// Elasticity when the drag exceeds constraints (0 = rigid, 1 = fully elastic).
    /// Accepts a uniform value (<c>DragElastic="0.5"</c>) or per-edge values via
    /// <c>BmDragElastic.Edges(right: 0.8, bottom: 0.8)</c>. Default: 0.35 on every edge.
    /// </summary>
    [Parameter] public BmDragElastic DragElastic { get; set; } = 0.35;

    /// <summary>
    /// CSS selector for a drag handle inside the element: the drag only starts when the
    /// pointer goes down on (or inside) a matching descendant, e.g. <c>DragHandle=".grip"</c>.
    /// </summary>
    [Parameter] public string? DragHandle { get; set; }

    /// <summary>
    /// Starts this element's drag from another element's pointer event - motion.dev's
    /// <c>useDragControls</c>. Pair with <see cref="DragListener"/> = false to make the
    /// controls the only trigger.
    /// </summary>
    [Parameter] public BmDragControls? DragControls { get; set; }

    /// <summary>
    /// Whether pressing the element itself starts the drag. Default: true. Set to false when
    /// the drag should only start via <see cref="DragControls"/>.
    /// </summary>
    [Parameter] public bool DragListener { get; set; } = true;

    /// <summary>Whether to apply momentum / inertia after release. Default: true.</summary>
    [Parameter] public bool DragMomentum { get; set; } = true;

    /// <summary>Spring back to the origin when released.</summary>
    [Parameter] public bool DragSnapToOrigin { get; set; }

    /// <summary>Lock the drag to the dominant movement axis once detected.</summary>
    [Parameter] public bool DragDirectionLock { get; set; }

    /// <summary>
    /// Whether a drag on this element also propagates to draggable ancestors. Default <c>false</c>
    /// (motion.dev's default): the drag stops propagation so a nested draggable doesn't also drag its
    /// draggable parent. Set <c>true</c> to let both move together.
    /// </summary>
    [Parameter] public bool DragPropagation { get; set; }

    /// <summary>Transition used for constraint snap-back / snap-to-origin. Defaults to a spring.</summary>
    [Parameter] public BmTransition? DragTransition { get; set; }

    // ── Layout ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Automatic FLIP layout animation: <c>Layout="true"</c> (position + size, with direct children
    /// counter-scaled and border-radius corrected so nothing distorts) or
    /// <c>Layout="BmLayout.Position"</c> (position only, the cheapest option).
    /// </summary>
    [Parameter] public BmLayout Layout { get; set; }

    /// <summary>
    /// Shared-element transitions: when this element mounts and another element with the same
    /// LayoutId was recently on screen, it animates (FLIP) from that element's bounding box -
    /// the tab-underline / card-to-detail idiom. Namespace ids with <see cref="BmotionLayoutGroup"/>.
    /// </summary>
    [Parameter] public string? LayoutId { get; set; }

    // ── Events ─────────────────────────────────────────────────────────────────
    [Parameter] public EventCallback OnHoverStart { get; set; }
    [Parameter] public EventCallback OnHoverEnd { get; set; }
    [Parameter] public EventCallback OnTapStart { get; set; }
    [Parameter] public EventCallback OnTap { get; set; }
    [Parameter] public EventCallback OnTapCancel { get; set; }
    [Parameter] public EventCallback OnFocusStart { get; set; }
    [Parameter] public EventCallback OnFocusEnd { get; set; }
    [Parameter] public EventCallback OnPanStart { get; set; }
    [Parameter] public EventCallback<BmPanInfo> OnPan { get; set; }
    [Parameter] public EventCallback OnPanEnd { get; set; }
    [Parameter] public EventCallback OnDragStart { get; set; }
    [Parameter] public EventCallback OnDrag { get; set; }
    [Parameter] public EventCallback OnDragEnd { get; set; }
    /// <summary>Fires when an Animate/State-driven animation starts; receives the resolved target props.</summary>
    [Parameter] public EventCallback<BmProps?> OnAnimationStart { get; set; }

    /// <summary>Fires when an Animate/State-driven animation completes naturally; receives the resolved target props.</summary>
    [Parameter] public EventCallback<BmProps?> OnAnimationComplete { get; set; }

    /// <summary>
    /// Called on every animation frame with the CSS declarations flushed to the element this
    /// frame. A plain delegate (not an <see cref="EventCallback"/>) by design: it runs inside
    /// the render loop and must not trigger a re-render per frame.
    /// </summary>
    [Parameter] public Action<IReadOnlyDictionary<string, string>>? OnUpdate { get; set; }
    [Parameter] public EventCallback OnViewportEnter { get; set; }
    [Parameter] public EventCallback OnViewportLeave { get; set; }

    // ── Internal state ─────────────────────────────────────────────────────────
    private string _id = $"bm-{Guid.NewGuid():N}";
    private DotNetObjectReference<Bmotion>? _dotnet;
    private bool _initialized;
    private bool _idChangeWarned;
    private bool _isExiting;
    private BmTarget? _prevAnimate;
    private BmotionVariantContext? _ownVariantCtx;
    private string? _prevInheritedVariant;
    private int _variantChildIndex = -1;
    private BmotionBoundingRect? _layoutSnapshot;
    // The style string most recently injected into the render tree, and the one we've reconciled
    // with the engine. When these diverge after init (the consumer authored a dynamic style that
    // changed), Blazor has rewritten the element's inline style, so we re-flush the engine's live
    // values on top to avoid resetting animated props.
    private string _pendingStyle = string.Empty;
    private string _committedStyle = string.Empty;
    // Signatures of the gesture-event flags and viewport options currently wired up in JS. Compared
    // each update so listeners/observers are re-attached only when the effective configuration
    // changes (gestures are otherwise wired once and would ignore later parameter changes).
    private string _eventFlagsSig = string.Empty;
    private string? _viewportSig;
    // Tracks whether WhileInView was set on the previous reconcile, so its removal can clear the
    // active in-view gesture layer even when the (option-only) viewport signature is unchanged.
    private bool _whileInViewSet;

    // ════════════════════════════════════════════════════════════════════════════
    // Rendering
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>The animate target: an explicit <see cref="Animate"/> wins over <see cref="State"/>.</summary>
    private BmTarget? EffectiveAnimate => Animate ?? (BmTarget?)State;

    /// <summary>The initial target: an explicit <see cref="Initial"/> wins over <see cref="InitialState"/>.</summary>
    private BmTarget? EffectiveInitial => Initial ?? (BmTarget?)InitialState;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // The component renders no element of its own; the consumer's child content authors the
        // element, and the rewriter injects the engine attributes (id + initial style +
        // pathLength) into its first root element while replaying the content's frames.
        var childContent = ChildContent
            ?? throw new InvalidOperationException(
                "<Bmotion> requires child content containing the HTML element to animate, e.g. " +
                "<Bmotion Animate=\"...\"><div class=\"box\" /></Bmotion>.");
        RenderFragment content = b =>
        {
            if (!BmotionChildContentRewriter.Render(b, childContent, PlanInjection))
                throw new InvalidOperationException(
                    "<Bmotion> child content has no root HTML element to animate. The first root " +
                    "node must be a plain element (not a component); wrap the content in one, " +
                    "e.g. <div>...</div>.");
        };

        if (Variants != null)
        {
            // Fall back to an inherited active variant from an ancestor so nested variant trees
            // propagate the active label when this component doesn't set its own State/Animate variant.
            var animateTarget = EffectiveAnimate;
            var active = animateTarget?.IsVariant == true ? animateTarget.Variant : VariantCtx?.ActiveVariant;
            // Mirror the ActiveVariant fallback: descendants inherit the initial variant label from
            // an ancestor when this node defines Variants without its own local Initial variant.
            var initialTarget = EffectiveInitial;
            var initial = initialTarget?.IsVariant == true ? initialTarget.Variant : VariantCtx?.InitialVariant;
            var stagger = Transition?.StaggerChildren ?? 0;
            var delayChildren = Transition?.DelayChildren ?? 0;

            // A cascaded context whose fields are mutated in place does NOT reliably notify
            // descendants (CascadingValue change-detection is reference-based). So when any cascaded
            // value actually changes, publish a NEW context instance - the changed reference forces
            // CascadingValue to re-notify children. The child-index counter is carried over so any
            // child registering after the swap still gets a stable stagger position.
            if (_ownVariantCtx is null ||
                _ownVariantCtx.ActiveVariant != active ||
                _ownVariantCtx.InitialVariant != initial ||
                !ReferenceEquals(_ownVariantCtx.Variants, Variants) ||
                _ownVariantCtx.StaggerChildren != stagger ||
                _ownVariantCtx.DelayChildren != delayChildren)
            {
                var previous = _ownVariantCtx;
                _ownVariantCtx = new BmotionVariantContext
                {
                    ActiveVariant = active,
                    InitialVariant = initial,
                    Variants = Variants,
                    StaggerChildren = stagger,
                    DelayChildren = delayChildren,
                };
                if (previous != null) _ownVariantCtx.SeedChildIndex(previous.NextChildIndex);
            }

            builder.OpenComponent<CascadingValue<BmotionVariantContext>>(0);
            builder.AddComponentParameter(1, "Value", _ownVariantCtx);
            builder.AddComponentParameter(2, "ChildContent", content);
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(3, content);
        }
    }

    /// <summary>
    /// Decides what the rewriter injects into the animated element, given what the consumer
    /// authored on it. Runs on every render (the consumer's style may be dynamic).
    /// </summary>
    private BmotionInjection PlanInjection(string tagName, string? authorId, string? authorStyle)
    {
        // Honor an id authored on the element (the Id parameter wins) so external CSS/JS can
        // target it. Adopted once, before interop init: the id is the element's engine identity,
        // so changing it after first render is unsupported.
        if (!_initialized && string.IsNullOrWhiteSpace(Id) && !string.IsNullOrWhiteSpace(authorId))
            _id = authorId!;

        // The rewriter merges the consumer's declarations after the motion style so they win
        // conflicts (CSS last-declaration-wins). Track the merge here to detect a consumer-driven
        // style rewrite on later renders (see OnAfterRenderAsync).
        var motionStyle = BuildInitialStyle();
        _pendingStyle = string.IsNullOrEmpty(authorStyle) ? motionStyle : motionStyle + authorStyle;

        // Auto-inject pathLength="1" (unless authored) so normalized [0,1] dasharray coordinates
        // work correctly on SVG shapes with path drawing.
        var addPathLength = _pathDrawableTags.Contains(tagName) && NeedsPathLength();
        return new BmotionInjection(_id, motionStyle, addPathLength);
    }

    private string BuildInitialStyle()
    {
        // The initial inline style exists only to avoid a flash of unstyled content before interop
        // initialises; it never changes after the first paint (the engine owns live styles from
        // then on), so compute it once and reuse the cached string on subsequent renders.
        if (_initialStyleCache != null) return _initialStyleCache;
        var props = ResolveProps(EffectiveInitial);
        if (props == null && EffectiveAnimate == null && VariantCtx?.InitialVariant is string initVariant)
            props = Variants?.Get(initVariant, Custom) ?? VariantCtx.Variants?.Get(initVariant, Custom);
        return _initialStyleCache = props?.ToCssStyleString() ?? string.Empty;
    }
    private string? _initialStyleCache;

    // ════════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════════════

    protected override void OnInitialized()
    {
        // Honor a user-supplied id so external CSS/JS (and programmatic APIs addressing elements
        // by selector) can target the element. Read once: the id is the element's engine identity,
        // so changing it after first render is unsupported.
        if (!string.IsNullOrWhiteSpace(Id)) _id = Id;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnet = DotNetObjectReference.Create(this);
            await InitialiseAsync();
            _initialized = true;
            // The initial injected style is what the engine seeded the DOM with; mark it reconciled.
            _committedStyle = _pendingStyle;
        }
        else if (_initialized)
        {
            await HandleParameterUpdateAsync();

            // FLIP: play layout animation after DOM settles
            if (_layoutSnapshot != null)
            {
                var snap = _layoutSnapshot;
                _layoutSnapshot = null;
                await PlayFlipAsync(snap);
            }

            // If a re-render rewrote the inline style attribute (the consumer authored a dynamic
            // style on the animated element), Blazor will have wiped the engine's live
            // transform/opacity/etc. The rewriter records the exact style it injects each render,
            // so divergence detection catches this; re-flush the current engine values on top so
            // animated state isn't visibly reset.
            if (_pendingStyle != _committedStyle)
            {
                _committedStyle = _pendingStyle;
                var live = Engine.GetCurrentStyles(_id);
                if (live is { Count: > 0 })
                    await Interop.ApplyStylesAsync(_id, live);
            }

            // Keep the shared-element registry current with this element's layout position.
            await RecordLayoutRectAsync();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (PresenceCtx is { IsExiting: true } && !_isExiting)
        {
            _isExiting = true;
            if (_initialized) await PlayExitAsync();
        }
        else if (_isExiting && PresenceCtx is not { IsExiting: true })
        {
            // The presence re-entered before/after this element finished exiting. Clear the
            // exiting flag (otherwise the component stays frozen forever) and force the enter
            // animation to replay by invalidating the cached previous target.
            _isExiting = false;
            _prevAnimate = null;
            _prevInheritedVariant = null;
            PresenceCtx?.Register(this); // re-register in case the context was reset
            // A popLayout exit pinned the element with position:absolute; put it back in the
            // layout flow now that the exit is cancelled (no-op when it was never popped).
            if (_initialized)
                try { await Interop.UnpopLayoutAsync(_id); } catch { /* cosmetic restore only */ }
        }

        // FLIP: snapshot BEFORE re-render
        if (_initialized && Layout.Enabled && !_isExiting)
            _layoutSnapshot = await Interop.GetBoundingRectAsync(_id);
    }

    private async Task InitialiseAsync()
    {
        // Probe the OS preference only when it can actually change ShouldReduceMotion()'s result:
        // a local <BmotionConfig ReduceMotion> wins outright, and Always/Never ignore the OS - so
        // detection there is a wasted interop round-trip. This mirrors ShouldReduceMotion().
        bool osPreferenceMatters = ConfigCtx?.ReduceMotion is null && Options.ReducedMotion switch
        {
            BmReducedMotionMode.User => true,
            BmReducedMotionMode.Always or BmReducedMotionMode.Never => false,
            _ => ConfigCtx is not null, // IgnoreUnlessConfigured: OS matters only inside a config
        };
        if (osPreferenceMatters)
            await Engine.EnsureReducedMotionDetectedAsync();

        // Register with C# engine (applies initial values synchronously)
        var initProps = ResolveProps(EffectiveInitial);
        Engine.RegisterElement(_id, initProps?.ToJsDictionary());
        Engine.SetOnFrame(_id, OnUpdate);
        ReconcileValueBindings();

        // Mark element in the DOM for JS bridge. The rewriter guarantees the id was rendered, so
        // not finding it means something outside Blazor removed or re-identified the element
        // (or another element claimed the same id) - fail loudly instead of animating nothing.
        if (!await Interop.RegisterElementAsync(_id))
            throw new InvalidOperationException(
                $"Bmotion could not find its animated element (id '{_id}') in the DOM. " +
                "Check for duplicate ids or external scripts mutating the element.");

        PresenceCtx?.Register(this);

        // Attach gesture listeners + viewport observation through the same reconciliation path used
        // on later updates, so enabling/disabling a gesture after first render is handled uniformly.
        ReconcileDragControls();
        await ReconcileEventListenersAsync();
        await ReconcileViewportAsync();

        // Shared-element transition: FLIP from wherever the previous LayoutId holder was.
        await HandleSharedLayoutMountAsync();

        // Start enter animation
        if (EffectiveAnimate != null)
        {
            var animateProps = ResolveProps(EffectiveAnimate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync(animateProps);
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(animateProps),
                    () => OnAnimationComplete.InvokeAsync(animateProps), setAsBase: true);
            }
        }
        else if (VariantCtx != null && (Variants != null || VariantCtx.Variants != null))
        {
            // Claim a stable stagger position on first render even when no variant is active yet,
            // so a parent that switches its variant later (the common hidden→visible toggle) still
            // staggers its children in render order instead of collapsing every delay to zero.
            _variantChildIndex = VariantCtx.RegisterChild();
            if (VariantCtx.ActiveVariant is string inheritedVariant)
            {
                _prevInheritedVariant = inheritedVariant;
                var props = Variants?.Get(inheritedVariant, Custom) ?? VariantCtx.Variants?.Get(inheritedVariant, Custom);
                if (props != null)
                    await Engine.AnimateToAsync(_id, props.ToJsDictionary(),
                        BuildEffectiveTransitionWithDelay(VariantCtx.GetChildDelay(_variantChildIndex), props),
                        setAsBase: true);
            }
        }

        _prevAnimate = EffectiveAnimate;
    }

    private async Task HandleParameterUpdateAsync()
    {
        if (_isExiting) return;

        // The id is the element's engine identity, adopted once at first render; changing the Id
        // parameter afterwards is unsupported and silently misbehaves. Warn once so it's diagnosable.
        if (!_idChangeWarned && !string.IsNullOrWhiteSpace(Id) && Id != _id)
        {
            _idChangeWarned = true;
            Logger?.LogWarning(
                "Bit.Bmotion: the Id parameter changed from '{OldId}' to '{NewId}' after first render. " +
                "The id is the element's immutable engine identity; the change is ignored.", _id, Id);
        }

        // Recovery: if the engine evicted this element after a driver fault (see
        // BmotionAnimationEngine.ComputeFrame), it silently stopped animating. Re-register and
        // re-seed it here so a subsequent parameter change brings it back to life.
        if (!Engine.IsRegistered(_id))
        {
            var seed = ResolveProps(EffectiveInitial);
            Engine.RegisterElement(_id, seed?.ToJsDictionary());
            _prevAnimate = null;            // force the animate below to replay
            _prevInheritedVariant = null;   // and the variant path too
        }

        // Keep the per-frame callback and motion-value bindings current with the latest parameters.
        Engine.SetOnFrame(_id, OnUpdate);
        ReconcileValueBindings();

        // Gesture listeners and viewport observation are wired once at init; re-wire them when the
        // set of needed events / viewport options changes so gestures enabled (or disabled) after
        // the first render actually take effect.
        ReconcileDragControls();
        await ReconcileEventListenersAsync();
        await ReconcileViewportAsync();

        if (!BmTarget.AreEquivalent(_prevAnimate, EffectiveAnimate))
        {
            var animateProps = ResolveProps(EffectiveAnimate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync(animateProps);
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(animateProps),
                    () => OnAnimationComplete.InvokeAsync(animateProps), setAsBase: true);
            }
            _prevAnimate = EffectiveAnimate;
        }
        // Not an "else": when the animate target transitions to null the block above still runs
        // (the targets differ) but applies nothing, so the inherited-variant fallback must be free
        // to run in the same update cycle rather than being deferred to a later rerender.
        if (EffectiveAnimate == null && (Variants != null || VariantCtx?.Variants != null))
        {
            var newVariant = VariantCtx?.ActiveVariant;
            if (newVariant != _prevInheritedVariant)
            {
                _prevInheritedVariant = newVariant;
                if (newVariant != null)
                {
                    var props = Variants?.Get(newVariant, Custom) ?? VariantCtx?.Variants?.Get(newVariant, Custom);
                    if (props != null)
                    {
                        // When this element rendered with a non-null Animate on first render it
                        // never claimed a stagger slot, so _variantChildIndex is still -1 here.
                        // Lazily claim one now (newVariant came from VariantCtx, so it's non-null)
                        // so GetChildDelay receives a real index instead of collapsing to zero.
                        if (_variantChildIndex < 0)
                            _variantChildIndex = VariantCtx!.RegisterChild();
                        double delay = VariantCtx!.GetChildDelay(_variantChildIndex);
                        await Engine.AnimateToAsync(_id, props.ToJsDictionary(),
                            BuildEffectiveTransitionWithDelay(delay, props), setAsBase: true);
                    }
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Exit & FLIP
    // ════════════════════════════════════════════════════════════════════════════

    internal async Task PlayExitAsync()
    {
        var exitProps = ResolveProps(Exit);
        if (exitProps != null)
        {
            // popLayout: pin the element at its current spot with position:absolute so siblings
            // reflow immediately, then play the exit on the popped element.
            if (PresenceCtx is { PopLayout: true })
            {
                var (x, y) = Engine.GetCurrentXY(_id);
                try { await Interop.PopLayoutAsync(_id, x, y); }
                catch { /* popping is cosmetic; the exit animation must still play */ }
            }
            await Engine.AnimateToAwaitAsync(_id, exitProps.ToJsDictionary(), BuildEffectiveTransition(exitProps));
        }
        PresenceCtx?.NotifyExitComplete(this);
    }

    private async Task PlayFlipAsync(BmotionBoundingRect snap)
    {
        var cur = await Interop.GetBoundingRectAsync(_id);
        if (cur == null) return;
        await PlayFlipFromAsync(snap, cur);
    }

    private async Task PlayFlipFromAsync(BmotionBoundingRect snap, BmotionBoundingRect cur)
    {
        double dx = snap.Left - cur.Left;
        double dy = snap.Top - cur.Top;
        // Position mode animates translation only, avoiding scale distortion on text/aspect-
        // ratio-sensitive content (motion.dev's layout="position").
        bool scaleToo = Layout.Mode != BmLayoutMode.Position;
        double sx = scaleToo && cur.Width > 0 ? snap.Width / cur.Width : 1;
        double sy = scaleToo && cur.Height > 0 ? snap.Height / cur.Height : 1;

        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5 && Math.Abs(sx - 1) < 0.005 && Math.Abs(sy - 1) < 0.005)
            return;

        var t = BuildEffectiveTransition();
        double dur = t?.Type == BmotionTransitionType.Spring ? 600 : (t?.Duration ?? 0.5) * 1000;
        string easing = t?.Type == BmotionTransitionType.Spring
            ? "cubic-bezier(0.14,1,0.34,1)"
            : BmEaseFunctions.ToCssString(t);
        string? finalT = Engine.GetCurrentTransformString(_id);

        // Pause the engine's per-frame transform writes for the duration of the FLIP so the rAF
        // loop and the WAAPI layout animation don't both write `transform` and tear each other.
        Engine.SuspendTransformWrites(_id, dur);
        // Rects measured while the FLIP transform is in flight would be skewed; pause registry
        // recording until it settles.
        _flipUntilMs = Environment.TickCount64 + (long)dur + 50;

        await Interop.PlayWaapiFlipAsync(_id, dx, dy, sx, sy, dur, easing, finalT);
    }

    // ── Shared-element transitions (LayoutId) ──────────────────────────────────

    // While a FLIP is in flight, getBoundingClientRect includes its transform; recording then
    // would poison the registry with mid-animation rects.
    private long _flipUntilMs;

    private string? EffectiveLayoutId
        => LayoutId is null ? null
         : LayoutGroupCtx?.Name is { Length: > 0 } group ? $"{group}:{LayoutId}" : LayoutId;

    /// <summary>
    /// On mount: if another element with the same LayoutId was recently on screen, FLIP from its
    /// recorded rect to this element's natural position. Always records the natural rect first -
    /// it is this element's true layout position, measured before any FLIP transform applies.
    /// </summary>
    private async Task HandleSharedLayoutMountAsync()
    {
        var layoutId = EffectiveLayoutId;
        if (layoutId is null) return;
        var cur = await Interop.GetBoundingRectAsync(_id);
        if (cur == null) return;
        var prev = LayoutRegistry.Get(layoutId);
        LayoutRegistry.Record(layoutId, cur);
        if (prev != null)
            await PlayFlipFromAsync(prev, cur);
    }

    private async Task RecordLayoutRectAsync()
    {
        var layoutId = EffectiveLayoutId;
        if (layoutId is null) return;
        if (Environment.TickCount64 < _flipUntilMs) return;
        var cur = await Interop.GetBoundingRectAsync(_id);
        if (cur != null) LayoutRegistry.Record(layoutId, cur);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Programmatic API
    // ════════════════════════════════════════════════════════════════════════════

    public async ValueTask AnimateAsync(BmProps props, BmTransition? transition = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(props);
        if (cancellationToken.IsCancellationRequested) return;
        // Apply CSS safe mode to the imperative API too, matching the declarative ResolveProps path.
        props = GuardCss(props)!;
        var values = props.ToJsDictionary();
        // Route the explicit transition through the same pipeline as the declarative path so it
        // inherits the global color space AND respects reduced motion / TransitionSpeed - a raw
        // transition.ToConfig() would bypass ShouldReduceMotion() and animate when it shouldn't.
        var config = BuildEffectiveTransition(props, transition);
        // Cancellation stops just the properties this call animates (Stop with null keys would
        // clobber unrelated animations on the same element). The registration is scoped to this
        // call so repeated calls with a long-lived token don't accumulate callbacks.
        using var registration = cancellationToken.CanBeCanceled
            ? cancellationToken.Register(() => Engine.Stop(_id, values.Keys.ToArray()))
            : default;
        await Engine.AnimateToAsync(_id, values, config);
    }

    public void Set(BmProps props)
    {
        ArgumentNullException.ThrowIfNull(props);
        Engine.SetInstant(_id, GuardCss(props)!.ToJsDictionary());
    }

    public async ValueTask SetAsync(BmProps props, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(props);
        if (cancellationToken.IsCancellationRequested) return;
        props = GuardCss(props)!;
        Engine.SetInstant(_id, props.ToJsDictionary());
        // Flush synchronous style update to DOM as individual declarations (never via cssText,
        // which would replace the element's entire inline style).
        var styles = props.ToCssStyleDictionary();
        if (styles.Count > 0)
            await Interop.ApplyStylesAsync(_id, styles);
    }

    public void Stop(params string[] properties) => Engine.Stop(_id, properties.Length > 0 ? properties : null);

    /// <summary>Pauses this element's animations in place.</summary>
    public void Pause() => Engine.SetPlaybackRate(_id, 0);

    /// <summary>Resumes this element's animations at normal speed.</summary>
    public void Resume() => Engine.SetPlaybackRate(_id, 1);

    /// <summary>Sets this element's playback rate: 1 = realtime, 0 = paused, 2 = twice as fast.</summary>
    public void SetPlaybackRate(double rate) => Engine.SetPlaybackRate(_id, rate);

    // ════════════════════════════════════════════════════════════════════════════
    // JS → C# callbacks (called from slim JS bridge)
    // ════════════════════════════════════════════════════════════════════════════

    // ── Hover ──────────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnPointerEnter()
    {
        var props = ResolveProps(WhileHover);
        if (props != null)
            await Engine.ActivateGestureLayerAsync(_id, "hover", props.ToJsDictionary(), BuildEffectiveTransition(props));
        await OnHoverStart.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnPointerLeave()
    {
        // Deactivate unconditionally: if WhileHover was cleared while the hover layer was still
        // active, a null guard here would strand the layer's styles. DeactivateGestureLayerAsync
        // is a no-op when no matching layer is active.
        await Engine.DeactivateGestureLayerAsync(_id, "hover");
        await OnHoverEnd.InvokeAsync();
    }

    // ── Tap ──────────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnPointerDown()
    {
        var props = ResolveProps(WhileTap);
        if (props != null)
            await Engine.ActivateGestureLayerAsync(_id, "tap", props.ToJsDictionary(), BuildEffectiveTransition(props));
        await OnTapStart.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnPointerUp(bool isInsideElement)
    {
        await Engine.DeactivateGestureLayerAsync(_id, "tap");
        if (isInsideElement) await OnTap.InvokeAsync();
        else await OnTapCancel.InvokeAsync(); // released outside the element ⇒ tap cancelled
    }

    [JSInvokable]
    public async Task OnPointerCancel()
    {
        await Engine.DeactivateGestureLayerAsync(_id, "tap");
        await OnTapCancel.InvokeAsync();
    }

    // ── Focus ──────────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnFocusIn()
    {
        var props = ResolveProps(WhileFocus);
        if (props != null)
            await Engine.ActivateGestureLayerAsync(_id, "focus", props.ToJsDictionary(), BuildEffectiveTransition(props));
        await OnFocusStart.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnFocusOut()
    {
        await Engine.DeactivateGestureLayerAsync(_id, "focus");
        await OnFocusEnd.InvokeAsync();
    }

    // ── Drag ──────────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnPointerDown_Drag()
    {
        var props = ResolveProps(WhileDrag);
        if (props != null)
            await Engine.ActivateGestureLayerAsync(_id, "drag", props.ToJsDictionary(), BuildEffectiveTransition(props));
        await OnDragStart.InvokeAsync();
    }

    /// <summary>Called synchronously from JS for drag position updates (Blazor WASM only).</summary>
    [JSInvokable] public void SetDragPosition(double x, double y) => Engine.SetDragPosition(_id, x, y);

    /// <summary>Called synchronously from JS to get current XY for drag start offset (Blazor WASM only).</summary>
    [JSInvokable]
    public BmotionXY GetCurrentXY()
    {
        var (x, y) = Engine.GetCurrentXY(_id);
        return new BmotionXY(x, y);
    }

    [JSInvokable] public async Task OnDragMove() => await OnDrag.InvokeAsync();

    [JSInvokable]
    public async Task OnPointerUp_Drag(double velX, double velY,
        double? boundLeft = null, double? boundRight = null, double? boundTop = null, double? boundBottom = null)
    {
        await Engine.DeactivateGestureLayerAsync(_id, "drag");

        if (DragSnapToOrigin)
        {
            // End the drag first (momentum off, no constraints) so the engine clears its dragging
            // state - the same cleanup the non-snap path below gets - before animating back.
            await Engine.EndDragAsync(_id, velX, velY, momentum: false, constraints: null, axis: null, snapTransition: null);
            var snapT = DragTransition?.ToConfig() ?? new BmotionTransitionConfig
                { Type = BmotionTransitionType.Spring, Stiffness = 400, Damping = 35 };
            await Engine.AnimateToAsync(_id,
                new Dictionary<string, object?> { ["x"] = 0.0, ["y"] = 0.0 }, snapT);
        }
        else
        {
            // JS reports the pixel bounds it actually constrained this drag with. Element-bounds
            // configs (Parent/Within) only exist in resolved form there, so prefer the reported
            // bounds; fall back to the static parameter when none arrived (e.g. no constraints).
            var constraints = boundLeft.HasValue || boundRight.HasValue || boundTop.HasValue || boundBottom.HasValue
                ? new BmDragConstraints { Left = boundLeft, Right = boundRight, Top = boundTop, Bottom = boundBottom }
                : DragConstraints;
            await Engine.EndDragAsync(
                _id, velX, velY, DragMomentum, constraints,
                Drag.Axis == BmDragAxis.Both ? null : Drag.Axis.ToString().ToLowerInvariant(),
                DragTransition?.ToConfig());
        }

        await OnDragEnd.InvokeAsync();
    }

    // ── Pan (pointer moves without moving the element) ──────────────────────────
    [JSInvokable]
    public async Task OnPanStart_() => await OnPanStart.InvokeAsync();

    [JSInvokable]
    public async Task OnPanMove(double pointX, double pointY,
        double deltaX, double deltaY, double offsetX, double offsetY,
        double velocityX, double velocityY)
    {
        if (OnPan.HasDelegate)
        {
            await OnPan.InvokeAsync(new BmPanInfo
            {
                Point    = new BmPoint { X = pointX,    Y = pointY },
                Delta    = new BmPoint { X = deltaX,    Y = deltaY },
                Offset   = new BmPoint { X = offsetX,   Y = offsetY },
                Velocity = new BmPoint { X = velocityX, Y = velocityY },
            });
        }
    }

    [JSInvokable]
    public async Task OnPanEnd_() => await OnPanEnd.InvokeAsync();

    // ── Viewport ──────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnIntersect(bool isIntersecting)
    {
        if (isIntersecting)
        {
            var props = ResolveProps(WhileInView);
            if (props != null)
                await Engine.ActivateGestureLayerAsync(_id, "inview", props.ToJsDictionary(), BuildEffectiveTransition(props));
            await OnViewportEnter.InvokeAsync();
        }
        else
        {
            if (!(Viewport?.Once ?? Once))
                await Engine.DeactivateGestureLayerAsync(_id, "inview");
            await OnViewportLeave.InvokeAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════════

    private static readonly HashSet<string> _pathDrawableTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "path", "circle", "ellipse", "line", "polyline", "polygon", "rect",
    };

    private bool NeedsPathLength() =>
        HasPathLength(EffectiveInitial) || HasPathLength(EffectiveAnimate) || HasPathLength(Exit) ||
        HasPathLength(WhileHover) || HasPathLength(WhileTap) || HasPathLength(WhileFocus) ||
        HasPathLength(WhileInView) || HasPathLength(WhileDrag);

    // Resolve the effective props (direct or variant-referenced) so pathLength is detected
    // whether the target carries Props directly or points at a variant label.
    private bool HasPathLength(BmTarget? t) =>
        ResolveProps(t)?.PathLength != null;

    private BmProps? ResolveProps(BmTarget? target)
    {
        if (target == null || target.IsDisabled) return null;
        if (target.HasProps) return GuardCss(target.Props);
        if (target.IsVariant)
        {
            var name = target.Variant!;
            return GuardCss(Variants?.Get(name, Custom) ?? VariantCtx?.Variants?.Get(name, Custom));
        }
        return null;
    }

    /// <summary>
    /// Opt-in CSS-injection safe mode (<see cref="BitBmotionOptions.CssSafeMode"/>): validates every
    /// string-valued CSS declaration a target carries, warning or throwing on a rejected value. A
    /// no-op (returns immediately) when the mode is Off, so the default path pays nothing.
    /// </summary>
    private BmProps? GuardCss(BmProps? props)
    {
        if (props is null || Options.CssSafeMode == BmCssSafeMode.Off) return props;
        foreach (var (prop, value) in props.EnumerateCssStringValues())
        {
            if (BmotionCssValidator.IsSafe(value)) continue;
            if (Options.CssSafeMode == BmCssSafeMode.Throw)
                throw new InvalidOperationException(
                    $"Bit.Bmotion CSS safe mode rejected the value for '{prop}': \"{value}\". " +
                    "It contains characters/sequences that could break out of the inline style.");
            Logger?.LogWarning(
                "Bit.Bmotion CSS safe mode: rejected value for '{Prop}' on element '{Id}': {Value}",
                prop, _id, value);
        }
        return props;
    }

    /// <summary>
    /// Resolves whether motion should be reduced for this element.
    /// <para>
    /// Reduced motion is <b>opt-in</b>: an element only reduces motion when it is inside a
    /// <see cref="BmotionConfig"/>. Within one, an explicit
    /// <see cref="BmotionConfigContext.ReduceMotion"/> value (true/false) always wins; when it is
    /// <c>null</c> the OS <c>prefers-reduced-motion</c> preference is respected. Elements with no
    /// surrounding config always animate, so the OS preference never silently disables animations
    /// an app didn't opt into.
    /// </para>
    /// </summary>
    private bool ShouldReduceMotion()
    {
        // A local <BmotionConfig ReduceMotion="true|false"> is always an explicit override.
        if (ConfigCtx?.ReduceMotion is bool configReduce) return configReduce;

        return Options.ReducedMotion switch
        {
            BmReducedMotionMode.Always => true,
            BmReducedMotionMode.Never => false,
            BmReducedMotionMode.User => Engine.OsPrefersReducedMotion,
            // IgnoreUnlessConfigured (back-compat): OS preference matters only inside a config.
            _ => ConfigCtx is not null && Engine.OsPrefersReducedMotion,
        };
    }

    // Under reduced motion we keep opacity and color animating (Motion's "user" semantics) while
    // transform/layout/dimension changes snap instantly. These are the properties that survive.
    private static readonly string[] _reducedRetainedProps =
        ["opacity", "backgroundColor", "color", "borderColor", "outlineColor", "fill", "stroke"];

    /// <summary>An instant (zero-duration) transition used when motion is reduced.</summary>
    private static BmotionTransitionConfig InstantTransition()
        => new() { Type = BmotionTransitionType.Tween, Duration = 0, Delay = 0 };

    /// <summary>
    /// Builds the reduced-motion transition: an instant base (so transforms/layout/dimensions snap)
    /// with per-property overrides that let opacity and color keep animating with their normal
    /// transition. This is softer and more correct than collapsing every property to instant.
    /// </summary>
    internal static BmotionTransitionConfig BuildReducedTransition(BmotionTransitionConfig? normal)
    {
        // A default-constructed config matches the engine's null-transition fallback (see
        // BmotionElementAnimationState.AnimateTo), so retained props animate with normal timing.
        var retained = normal ?? new BmotionTransitionConfig();
        var reduced = InstantTransition();
        reduced.Properties = new Dictionary<string, BmotionTransitionConfig>(StringComparer.Ordinal);
        foreach (var key in _reducedRetainedProps)
            reduced.Properties[key] = retained;
        return reduced;
    }

    private BmotionTransitionConfig? BuildEffectiveTransition(BmProps? props = null, BmTransition? explicitTransition = null)
    {
        var normal = BuildNormalTransition(props, explicitTransition);
        // Reduced motion: snap transforms/layout but keep opacity/color animating.
        return ShouldReduceMotion() ? BuildReducedTransition(normal) : normal;
    }

    private BmotionTransitionConfig? BuildNormalTransition(BmProps? props, BmTransition? explicitTransition = null)
    {
        // Resolution order: an explicit transition (programmatic AnimateAsync) wins, then the one
        // embedded in the target, then the component's Transition, then the <BmotionConfig> default.
        var t = explicitTransition ?? props?.Transition ?? Transition ?? ConfigCtx?.DefaultTransition;
        if (t == null) return null;
        // ToConfig returns a fresh instance (safe to mutate below) and inherits the global
        // <BmotionConfig> color space wherever this transition - or any per-property override -
        // didn't set its own, so a per-property color override doesn't silently fall back to sRGB
        // under a global OKLab config (the engine uses the per-key config verbatim, see
        // BmotionElementAnimationState.AnimateTo).
        var config = t.ToConfig(ConfigCtx?.ColorSpace);
        if (ConfigCtx?.TransitionSpeed is double speed && speed != 1.0)
        {
            // TransitionSpeed is a rate: 2 = twice as fast, 0.5 = half speed, <= 0 = instant.
            // Scale every time-based field so the whole animation is consistently sped up /
            // slowed down - not just the tween duration (which left delays and duration-based
            // springs out of sync with the requested speed).
            if (speed <= 0)
            {
                config.Duration = 0;
                config.Delay = 0;
                config.RepeatDelay = 0;
                if (config.VisualDuration.HasValue) config.VisualDuration = 0;
            }
            else
            {
                config.Duration /= speed;
                config.Delay /= speed;
                config.RepeatDelay /= speed;
                if (config.VisualDuration.HasValue) config.VisualDuration /= speed;
            }
        }
        return config;
    }

    private BmotionTransitionConfig BuildEffectiveTransitionWithDelay(double extraDelay, BmProps? props = null)
    {
        // Reduced motion: keep opacity/color animating but skip the stagger delay (accessibility).
        if (ShouldReduceMotion()) return BuildReducedTransition(BuildNormalTransition(props));

        // BuildNormalTransition returns a fresh instance (or none existed), so mutating is safe.
        var t = BuildNormalTransition(props) ?? new BmotionTransitionConfig();
        if (extraDelay > 0) t.Delay += extraDelay;
        return t;
    }

    private Dictionary<string, object?> BuildEventFlags()
    {
        var d = new Dictionary<string, object?>();
        if (WhileHover != null || OnHoverStart.HasDelegate || OnHoverEnd.HasDelegate) d["hover"] = true;
        if (WhileTap != null || OnTapStart.HasDelegate || OnTap.HasDelegate || OnTapCancel.HasDelegate) d["tap"] = true;
        if (WhileFocus != null || OnFocusStart.HasDelegate || OnFocusEnd.HasDelegate) d["focus"] = true;
        if (OnPanStart.HasDelegate || OnPan.HasDelegate || OnPanEnd.HasDelegate) d["pan"] = true;
        if (Drag.Enabled)
        {
            d["drag"] = true;
            if (Drag.Axis != BmDragAxis.Both) d["dragAxis"] = Drag.Axis.ToString().ToLowerInvariant();
            // ToJsObject sanitises each edge (finite, clamped to [0, 1]) so the JS elasticity
            // math never receives NaN/±Infinity.
            d["dragElastic"] = DragElastic.ToJsObject();
            if (DragConstraints != null) d["dragConstraints"] = DragConstraints.ToJsObject();
            if (DragDirectionLock) d["dragDirectionLock"] = true;
            if (DragPropagation) d["dragPropagation"] = true;
            if (!string.IsNullOrWhiteSpace(DragHandle)) d["dragHandle"] = DragHandle;
            if (!DragListener) d["dragListener"] = false;
        }
        return d;
    }

    // ── Drag controls ──────────────────────────────────────────────────────────

    private BmDragControls? _attachedDragControls;

    /// <summary>Keeps the (single) attached <see cref="BmDragControls"/> current with the parameter.</summary>
    private void ReconcileDragControls()
    {
        if (ReferenceEquals(_attachedDragControls, DragControls)) return;
        _attachedDragControls?.Detach(this);
        _attachedDragControls = DragControls;
        _attachedDragControls?.Attach(this);
    }

    /// <summary>Starts this element's drag from an external pointer event (see <see cref="BmDragControls"/>).</summary>
    internal ValueTask StartDragAsync(PointerEventArgs e)
        => _initialized
            ? Interop.StartDragAsync(_id, e.PointerId, e.ClientX, e.ClientY)
            : ValueTask.CompletedTask;

    /// <summary>
    /// Re-wires the JS gesture listeners when the effective event set changes. Attaching always
    /// runs the JS-side cleanup first, so passing an empty set also safely detaches everything.
    /// </summary>
    private async Task ReconcileEventListenersAsync()
    {
        var events = BuildEventFlags();
        var sig = SignatureOf(events);
        if (sig == _eventFlagsSig) return;
        _eventFlagsSig = sig;
        await Interop.AttachEventListenersAsync(_id, events, _dotnet!);
    }

    /// <summary>Re-observes (or stops observing) the viewport when the effective options change.</summary>
    private async Task ReconcileViewportAsync()
    {
        // The viewport signature intentionally ignores WhileInView (it only tracks observation
        // options). So if WhileInView is cleared while other viewport callbacks keep observation
        // alive, the signature is unchanged and the early-return below would skip reconciliation,
        // stranding an already-active in-view layer. Detect that transition explicitly and clear it.
        bool whileInViewSet = WhileInView != null;
        if (_whileInViewSet && !whileInViewSet)
            await Engine.DeactivateGestureLayerAsync(_id, "inview");
        else if (!_whileInViewSet && whileInViewSet)
            // WhileInView was just enabled. If observation is already active for other viewport
            // callbacks the (option-only) signature is unchanged, so the early-return below would
            // skip activating the in-view layer for an element that may already be visible. Drop
            // the cached signature to force re-observation, which makes JS re-report the current
            // intersection state (and activates the layer only when actually in view).
            _viewportSig = null;
        _whileInViewSet = whileInViewSet;

        var sig = BuildViewportSignature();
        if (sig == _viewportSig) return;
        _viewportSig = sig;
        if (sig == null)
        {
            await Interop.UnobserveViewportAsync(_id);
            // Unobserving stops future intersect callbacks but doesn't undo an already-active
            // in-view layer, so clear it here to avoid leaving inview styles stuck on the element.
            await Engine.DeactivateGestureLayerAsync(_id, "inview");
            return;
        }
        if (Viewport != null)
            await Interop.ObserveViewportWithOptionsAsync(_id, _dotnet!, Viewport);
        else
            await Interop.ObserveViewportAsync(_id, _dotnet!, Once);
    }

    private string? BuildViewportSignature()
    {
        bool needed = WhileInView != null || OnViewportEnter.HasDelegate || OnViewportLeave.HasDelegate;
        if (!needed) return null;
        return Viewport != null
            ? $"opt|{Viewport.Once}|{Viewport.Margin}|{Viewport.Amount}"
            : $"once|{Once}";
    }

    // ── Motion-value bindings ──────────────────────────────────────────────────

    private readonly List<IDisposable> _valueSubscriptions = new();
    private Dictionary<string, BmValue<double>>? _boundValues;
    private Dictionary<string, BmValue<string>>? _boundStringValues;

    /// <summary>
    /// (Re)subscribes to the bound motion values when the <see cref="Values"/> or
    /// <see cref="StringValues"/> dictionary instance changes. Each change writes straight into
    /// the engine (no component re-render); the engine's dirty-flag batching flushes it on the
    /// next frame.
    /// </summary>
    private void ReconcileValueBindings()
    {
        if (ReferenceEquals(_boundValues, Values) && ReferenceEquals(_boundStringValues, StringValues))
            return;

        foreach (var subscription in _valueSubscriptions) subscription.Dispose();
        _valueSubscriptions.Clear();
        _boundValues = Values;
        _boundStringValues = StringValues;

        if (Values != null)
            foreach (var (key, value) in Values)
            {
                var propertyKey = key;
                _valueSubscriptions.Add(value.Subscribe(v =>
                    Engine.SetInstant(_id, new Dictionary<string, object?> { [propertyKey] = v })));
                // Seed the current value so the element reflects it before the first change.
                Engine.SetInstant(_id, new Dictionary<string, object?> { [propertyKey] = value.Value });
            }

        if (StringValues != null)
            foreach (var (key, value) in StringValues)
            {
                var propertyKey = key;
                _valueSubscriptions.Add(value.Subscribe(v =>
                    Engine.SetInstant(_id, new Dictionary<string, object?> { [propertyKey] = v })));
                Engine.SetInstant(_id, new Dictionary<string, object?> { [propertyKey] = value.Value });
            }
    }

    /// <summary>Builds a stable, order-independent string signature for an event-flags dictionary.</summary>
    private static string SignatureOf(Dictionary<string, object?> d)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var key in d.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            sb.Append(key).Append('=');
            AppendValue(sb, d[key]);
            sb.Append(';');
        }
        return sb.ToString();
    }

    private static void AppendValue(System.Text.StringBuilder sb, object? value)
    {
        switch (value)
        {
            case null:
                sb.Append("null");
                break;
            case double dbl:
                sb.Append(BmotionCssFormat.Num(dbl));
                break;
            case bool b:
                sb.Append(b ? "1" : "0");
                break;
            case IDictionary<string, object?> nested:
                sb.Append('{');
                foreach (var key in nested.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    sb.Append(key).Append(':');
                    AppendValue(sb, nested[key]);
                    sb.Append(',');
                }
                sb.Append('}');
                break;
            default:
                sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Dispose
    // ════════════════════════════════════════════════════════════════════════════

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in _valueSubscriptions) subscription.Dispose();
        _valueSubscriptions.Clear();
        _attachedDragControls?.Detach(this);
        PresenceCtx?.Unregister(this);
        Engine.UnregisterElement(_id);
        try { await Interop.UnregisterElementAsync(_id); } catch { /* ignore during teardown */ }
        try { await Interop.UnobserveViewportAsync(_id); } catch { /* ignore during teardown */ }
        _dotnet?.Dispose();
    }
}
