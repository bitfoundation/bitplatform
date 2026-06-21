using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bmotion;
/// <summary>
/// The primary animation component - a drop-in replacement for any HTML element.
/// Animation math runs in the C# <see cref="BmotionAnimationEngine"/>; JS is used only
/// for DOM style mutation, pointer/focus events, viewport observation and FLIP.
/// </summary>
public sealed class Bmotion : ComponentBase, IAsyncDisposable
{
    // ── Injected services ──────────────────────────────────────────────────────
    [Inject] private BmotionAnimationEngine Engine { get; set; } = null!;
    [Inject] private BmotionInterop Interop { get; set; } = null!;

    // ── Cascaded contexts ──────────────────────────────────────────────────────
    [CascadingParameter] private BmotionPresenceContext? PresenceCtx { get; set; }
    [CascadingParameter] private BmotionVariantContext? VariantCtx { get; set; }
    [CascadingParameter] private BmotionConfigContext? ConfigCtx { get; set; }

    // ── Core rendering parameters ────────────────────────────────────────────
    [Parameter] public string Tag { get; set; } = "div";
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Animation targets ──────────────────────────────────────────────────────
    [Parameter] public BmotionAnimationTarget? Initial { get; set; }
    [Parameter] public BmotionAnimationTarget? Animate { get; set; }
    [Parameter] public BmotionAnimationTarget? Exit { get; set; }

    // ── Gesture states ─────────────────────────────────────────────────────────
    [Parameter] public BmotionAnimationTarget? WhileHover { get; set; }
    [Parameter] public BmotionAnimationTarget? WhileTap { get; set; }
    [Parameter] public BmotionAnimationTarget? WhileFocus { get; set; }
    [Parameter] public BmotionAnimationTarget? WhileDrag { get; set; }
    [Parameter] public BmotionAnimationTarget? WhileInView { get; set; }

    /// <summary>
    /// If <c>true</c>, <see cref="WhileInView"/> fires only once and never deactivates.
    /// Shorthand for <c>Viewport = new BmotionViewportOptions { Once = true }</c>.
    /// </summary>
    [Parameter] public bool Once { get; set; }

    /// <summary>
    /// Advanced viewport options for <see cref="WhileInView"/> (margin, amount, once).
    /// When set, <see cref="Once"/> is ignored in favour of <c>Viewport.Once</c>.
    /// </summary>
    [Parameter] public BmotionViewportOptions? Viewport { get; set; }

    // ── Transition ─────────────────────────────────────────────────────────────
    [Parameter] public BmotionTransitionConfig? Transition { get; set; }

    // ── Variants ─────────────────────────────────────────────────────────────
    [Parameter] public BmotionMotionVariants? Variants { get; set; }

    // ── Drag ─────────────────────────────────────────────────────────────────
    [Parameter] public bool Drag { get; set; }
    [Parameter] public BmotionDragOptions? DragOptions { get; set; }

    // ── Layout ─────────────────────────────────────────────────────────────────
    [Parameter] public bool Layout { get; set; }

    // ── Events ─────────────────────────────────────────────────────────────────
    [Parameter] public EventCallback OnHoverStart { get; set; }
    [Parameter] public EventCallback OnHoverEnd { get; set; }
    [Parameter] public EventCallback OnTapStart { get; set; }
    [Parameter] public EventCallback OnTap { get; set; }
    [Parameter] public EventCallback OnTapCancel { get; set; }
    [Parameter] public EventCallback OnFocusStart { get; set; }
    [Parameter] public EventCallback OnFocusEnd { get; set; }
    [Parameter] public EventCallback OnPanStart { get; set; }
    [Parameter] public EventCallback<BmotionPanInfo> OnPan { get; set; }
    [Parameter] public EventCallback OnPanEnd { get; set; }
    [Parameter] public EventCallback OnDragStart { get; set; }
    [Parameter] public EventCallback OnDrag { get; set; }
    [Parameter] public EventCallback OnDragEnd { get; set; }
    [Parameter] public EventCallback OnAnimationStart { get; set; }
    [Parameter] public EventCallback OnAnimationComplete { get; set; }
    [Parameter] public EventCallback OnViewportEnter { get; set; }
    [Parameter] public EventCallback OnViewportLeave { get; set; }

    // ── Internal state ─────────────────────────────────────────────────────────
    private readonly string _id = $"bm-{Guid.NewGuid():N}";
    private ElementReference _ref;
    private DotNetObjectReference<Bmotion>? _dotnet;
    private bool _initialized;
    private bool _isExiting;
    private BmotionAnimationTarget? _prevAnimate;
    private BmotionVariantContext? _ownVariantCtx;
    private string? _prevInheritedVariant;
    private int _variantChildIndex = -1;
    private BmotionBoundingRect? _layoutSnapshot;
    // The style string most recently emitted into the render tree, and the one we've reconciled
    // with the engine. When these diverge after init, Blazor has rewritten the element's inline
    // style, so we re-flush the engine's live values on top to avoid resetting animated props.
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

    /// <summary>The element tag to render, normalized to "div" when null or blank so
    /// <see cref="RenderTreeBuilder.OpenElement"/> always receives a valid tag name.</summary>
    private string EffectiveTag => string.IsNullOrWhiteSpace(Tag) ? "div" : Tag;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Sequence numbers are fixed literals (one per logical slot) rather than a running counter.
        // Blazor's diffing assumes stable sequence numbers; computing them dynamically alongside
        // conditional attributes shifts the numbers between renders and degrades diffing.
        builder.OpenElement(0, EffectiveTag);
        builder.AddAttribute(1, "id", _id);

        if (AdditionalAttributes != null)
            builder.AddMultipleAttributes(2,
                AdditionalAttributes.Where(kvp => !string.Equals(kvp.Key, "id", StringComparison.OrdinalIgnoreCase)));

        // Auto-inject pathLength="1" so normalized [0,1] dasharray coordinates work correctly
        if (NeedsPathLengthAttr())
            builder.AddAttribute(3, "pathLength", "1");

        if (!string.IsNullOrEmpty(Class))
            builder.AddAttribute(4, "class", Class);

        var motionStyle = BuildInitialStyle();
        var combinedStyle = string.IsNullOrEmpty(Style) ? motionStyle : motionStyle + Style;
        _pendingStyle = combinedStyle ?? string.Empty;
        if (!string.IsNullOrEmpty(combinedStyle))
            builder.AddAttribute(5, "style", combinedStyle);

        builder.AddElementReferenceCapture(6, r => _ref = r);

        if (Variants != null)
        {
            // Fall back to an inherited active variant from an ancestor so nested variant trees
            // propagate the active label when this component doesn't set its own Animate.Variant.
            var active = Animate?.IsVariant == true ? Animate.Variant : VariantCtx?.ActiveVariant;
            // Mirror the ActiveVariant fallback: descendants inherit the initial variant label from
            // an ancestor when this node defines Variants without its own local Initial variant.
            var initial = Initial?.IsVariant == true ? Initial.Variant : VariantCtx?.InitialVariant;
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

            builder.OpenComponent<CascadingValue<BmotionVariantContext>>(7);
            builder.AddComponentParameter(8, "Value", _ownVariantCtx);
            builder.AddComponentParameter(9, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(10, ChildContent);
        }
        builder.CloseElement();
    }

    private string BuildInitialStyle()
    {
        // The initial inline style exists only to avoid a flash of unstyled content before interop
        // initialises; it never changes after the first paint (the engine owns live styles from
        // then on), so compute it once and reuse the cached string on subsequent renders.
        if (_initialStyleCache != null) return _initialStyleCache;
        var props = ResolveProps(Initial);
        if (props == null && Animate == null && VariantCtx?.InitialVariant is string initVariant)
            props = Variants?.Get(initVariant) ?? VariantCtx.Variants?.Get(initVariant);
        return _initialStyleCache = props?.ToCssStyleString() ?? string.Empty;
    }
    private string? _initialStyleCache;

    // ════════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════════════

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnet = DotNetObjectReference.Create(this);
            await InitialiseAsync();
            _initialized = true;
            // The initial inline style is what the engine seeded the DOM with; mark it reconciled.
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

            // If a re-render rewrote the inline style attribute (e.g. the consumer changed Style),
            // Blazor will have wiped the engine's live transform/opacity/etc. Re-flush the current
            // engine values on top so animated state isn't visibly reset.
            if (_pendingStyle != _committedStyle)
            {
                _committedStyle = _pendingStyle;
                var live = Engine.GetCurrentStyles(_id);
                if (live is { Count: > 0 })
                    await Interop.ApplyStylesAsync(_id, live);
            }
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
        }

        // FLIP: snapshot BEFORE re-render
        if (_initialized && Layout && !_isExiting)
            _layoutSnapshot = await Interop.GetBoundingRectAsync(_id);
    }

    private async Task InitialiseAsync()
    {
        // Reduced-motion is opt-in: only probe the OS preference when this element is
        // inside a <BmotionConfig>. Elements without a config always animate normally.
        if (ConfigCtx is not null)
            await Engine.EnsureReducedMotionDetectedAsync();

        // Register with C# engine (applies initial values synchronously)
        var initProps = ResolveProps(Initial);
        Engine.RegisterElement(_id, initProps?.ToJsDictionary());

        // Mark element in the DOM for JS bridge
        await Interop.RegisterElementAsync(_id);

        PresenceCtx?.Register(this);

        // Attach gesture listeners + viewport observation through the same reconciliation path used
        // on later updates, so enabling/disabling a gesture after first render is handled uniformly.
        await ReconcileEventListenersAsync();
        await ReconcileViewportAsync();

        // Start enter animation
        if (Animate != null)
        {
            var animateProps = ResolveProps(Animate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync();
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(),
                    () => OnAnimationComplete.InvokeAsync(), setAsBase: true);
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
                var props = Variants?.Get(inheritedVariant) ?? VariantCtx.Variants?.Get(inheritedVariant);
                if (props != null)
                    await Engine.AnimateToAsync(_id, props.ToJsDictionary(),
                        BuildEffectiveTransitionWithDelay(VariantCtx.GetChildDelay(_variantChildIndex)),
                        setAsBase: true);
            }
        }

        _prevAnimate = Animate;
    }

    private async Task HandleParameterUpdateAsync()
    {
        if (_isExiting) return;

        // Recovery: if the engine evicted this element after a driver fault (see
        // BmotionAnimationEngine.ComputeFrame), it silently stopped animating. Re-register and
        // re-seed it here so a subsequent parameter change brings it back to life.
        if (!Engine.IsRegistered(_id))
        {
            var seed = ResolveProps(Initial);
            Engine.RegisterElement(_id, seed?.ToJsDictionary());
            _prevAnimate = null;            // force the animate below to replay
            _prevInheritedVariant = null;   // and the variant path too
        }

        // Gesture listeners and viewport observation are wired once at init; re-wire them when the
        // set of needed events / viewport options changes so gestures enabled (or disabled) after
        // the first render actually take effect.
        await ReconcileEventListenersAsync();
        await ReconcileViewportAsync();

        if (!BmotionAnimationTarget.AreEquivalent(_prevAnimate, Animate))
        {
            var animateProps = ResolveProps(Animate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync();
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(),
                    () => OnAnimationComplete.InvokeAsync(), setAsBase: true);
            }
            _prevAnimate = Animate;
        }
        // Not an "else": when Animate transitions to null the block above still runs (the targets
        // differ) but applies nothing, so the inherited-variant fallback must be free to run in the
        // same update cycle rather than being deferred to a later rerender.
        if (Animate == null && (Variants != null || VariantCtx?.Variants != null))
        {
            var newVariant = VariantCtx?.ActiveVariant;
            if (newVariant != _prevInheritedVariant)
            {
                _prevInheritedVariant = newVariant;
                if (newVariant != null)
                {
                    var props = Variants?.Get(newVariant) ?? VariantCtx?.Variants?.Get(newVariant);
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
                            BuildEffectiveTransitionWithDelay(delay), setAsBase: true);
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
            await Engine.AnimateToAwaitAsync(_id, exitProps.ToJsDictionary(), BuildEffectiveTransition());
        PresenceCtx?.NotifyExitComplete(this);
    }

    private async Task PlayFlipAsync(BmotionBoundingRect snap)
    {
        var cur = await Interop.GetBoundingRectAsync(_id);
        if (cur == null) return;

        double dx = snap.Left - cur.Left;
        double dy = snap.Top - cur.Top;
        double sx = cur.Width > 0 ? snap.Width / cur.Width : 1;
        double sy = cur.Height > 0 ? snap.Height / cur.Height : 1;

        if (Math.Abs(dx) < 0.5 && Math.Abs(dy) < 0.5 && Math.Abs(sx - 1) < 0.005 && Math.Abs(sy - 1) < 0.005)
            return;

        var t = BuildEffectiveTransition();
        double dur = t?.Type == BmotionTransitionType.Spring ? 600 : (t?.Duration ?? 0.5) * 1000;
        string easing = t?.Type == BmotionTransitionType.Spring
            ? "cubic-bezier(0.14,1,0.34,1)"
            : BmotionEasingFunctions.ToCssString(t);
        string? finalT = Engine.GetCurrentTransformString(_id);

        // Pause the engine's per-frame transform writes for the duration of the FLIP so the rAF
        // loop and the WAAPI layout animation don't both write `transform` and tear each other.
        Engine.SuspendTransformWrites(_id, dur);

        await Interop.PlayWaapiFlipAsync(_id, dx, dy, sx, sy, dur, easing, finalT);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Programmatic API
    // ════════════════════════════════════════════════════════════════════════════

    public async ValueTask AnimateAsync(BmotionAnimationProps props, BmotionTransitionConfig? transition = null)
    {
        transition ??= BuildEffectiveTransition();
        await Engine.AnimateToAsync(_id, props.ToJsDictionary(), transition);
    }

    public void Set(BmotionAnimationProps props) => Engine.SetInstant(_id, props.ToJsDictionary());

    public async ValueTask SetAsync(BmotionAnimationProps props)
    {
        Engine.SetInstant(_id, props.ToJsDictionary());
        // Flush synchronous style update to DOM as individual declarations (never via cssText,
        // which would replace the element's entire inline style).
        var styles = props.ToCssStyleDictionary();
        if (styles.Count > 0)
            await Interop.ApplyStylesAsync(_id, styles);
    }

    public void Stop(params string[] properties) => Engine.Stop(_id, properties.Length > 0 ? properties : null);

    // ════════════════════════════════════════════════════════════════════════════
    // JS → C# callbacks (called from slim JS bridge)
    // ════════════════════════════════════════════════════════════════════════════

    // ── Hover ──────────────────────────────────────────────────────────────────
    [JSInvokable]
    public async Task OnPointerEnter()
    {
        var props = ResolveProps(WhileHover);
        if (props != null)
            await Engine.ActivateGestureLayerAsync(_id, "hover", props.ToJsDictionary(), BuildEffectiveTransition());
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
            await Engine.ActivateGestureLayerAsync(_id, "tap", props.ToJsDictionary(), BuildEffectiveTransition());
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
            await Engine.ActivateGestureLayerAsync(_id, "focus", props.ToJsDictionary(), BuildEffectiveTransition());
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
            await Engine.ActivateGestureLayerAsync(_id, "drag", props.ToJsDictionary(), BuildEffectiveTransition());
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
    public async Task OnPointerUp_Drag(double velX, double velY)
    {
        await Engine.DeactivateGestureLayerAsync(_id, "drag");

        var dragOpt = DragOptions ?? new BmotionDragOptions();

        if (dragOpt.SnapToOrigin)
        {
            var snapT = dragOpt.SnapTransition ?? new BmotionTransitionConfig
                { Type = BmotionTransitionType.Spring, Stiffness = 400, Damping = 35 };
            await Engine.AnimateToAsync(_id,
                new Dictionary<string, object?> { ["x"] = 0.0, ["y"] = 0.0 }, snapT);
        }
        else
        {
            await Engine.EndDragAsync(
                _id, velX, velY, dragOpt.Momentum, dragOpt.Constraints,
                dragOpt.Axis == BmotionDragAxis.Both ? null : dragOpt.Axis.ToString().ToLowerInvariant(),
                dragOpt.SnapTransition);
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
            await OnPan.InvokeAsync(new BmotionPanInfo
            {
                Point    = new BmotionPointInfo { X = pointX,    Y = pointY },
                Delta    = new BmotionPointInfo { X = deltaX,    Y = deltaY },
                Offset   = new BmotionPointInfo { X = offsetX,   Y = offsetY },
                Velocity = new BmotionPointInfo { X = velocityX, Y = velocityY },
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
                await Engine.ActivateGestureLayerAsync(_id, "inview", props.ToJsDictionary(), BuildEffectiveTransition());
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

    private bool NeedsPathLengthAttr() =>
        _pathDrawableTags.Contains(EffectiveTag) &&
        (AdditionalAttributes == null ||
         !AdditionalAttributes.Keys.Any(k => string.Equals(k, "pathLength", StringComparison.OrdinalIgnoreCase))) &&
        (HasPathLength(Initial) || HasPathLength(Animate) || HasPathLength(Exit) ||
         HasPathLength(WhileHover) || HasPathLength(WhileTap) || HasPathLength(WhileFocus) ||
         HasPathLength(WhileInView) || HasPathLength(WhileDrag));

    // Resolve the effective props (direct or variant-referenced) so pathLength is detected
    // whether the target carries Props directly or points at a variant label.
    private bool HasPathLength(BmotionAnimationTarget? t) =>
        ResolveProps(t)?.PathLength != null;

    private BmotionAnimationProps? ResolveProps(BmotionAnimationTarget? target)
    {
        if (target == null || target.IsDisabled) return null;
        if (target.HasProps) return target.Props;
        if (target.IsVariant)
        {
            var name = target.Variant!;
            return Variants?.Get(name) ?? VariantCtx?.Variants?.Get(name);
        }
        return null;
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
        if (ConfigCtx is null) return false;
        return ConfigCtx.ReduceMotion ?? Engine.OsPrefersReducedMotion;
    }

    /// <summary>An instant (zero-duration) transition used when motion is reduced.</summary>
    private static BmotionTransitionConfig InstantTransition()
        => new() { Type = BmotionTransitionType.Tween, Duration = 0, Delay = 0 };

    private BmotionTransitionConfig? BuildEffectiveTransition()
    {
        // Reduced motion: collapse every animation to an instant state change.
        if (ShouldReduceMotion()) return InstantTransition();

        var t = Transition ?? ConfigCtx?.DefaultTransition;
        if (t == null) return null;
        if (ConfigCtx?.TransitionSpeed is double speed && speed != 1.0)
        {
            t = t.Clone();
            // Scale every time-based field so the whole animation is consistently sped up /
            // slowed down - not just the tween duration (which left delays and duration-based
            // springs out of sync with the requested speed).
            t.Duration *= speed;
            t.Delay *= speed;
            t.RepeatDelay *= speed;
            if (t.VisualDuration.HasValue) t.VisualDuration *= speed;
        }
        return t;
    }

    private BmotionTransitionConfig BuildEffectiveTransitionWithDelay(double extraDelay)
    {
        // Reduced motion stays instant - stagger delays are skipped too.
        if (ShouldReduceMotion()) return InstantTransition();

        var t = BuildEffectiveTransition() ?? new BmotionTransitionConfig();
        if (extraDelay <= 0) return t;
        t = t.Clone();
        t.Delay += extraDelay;
        return t;
    }

    private Dictionary<string, object?> BuildEventFlags()
    {
        var d = new Dictionary<string, object?>();
        if (WhileHover != null || OnHoverStart.HasDelegate || OnHoverEnd.HasDelegate) d["hover"] = true;
        if (WhileTap != null || OnTapStart.HasDelegate || OnTap.HasDelegate || OnTapCancel.HasDelegate) d["tap"] = true;
        if (WhileFocus != null || OnFocusStart.HasDelegate || OnFocusEnd.HasDelegate) d["focus"] = true;
        if (OnPanStart.HasDelegate || OnPan.HasDelegate || OnPanEnd.HasDelegate) d["pan"] = true;
        if (Drag)
        {
            d["drag"] = true;
            var dragOpt = DragOptions ?? new BmotionDragOptions();
            if (dragOpt.Axis != BmotionDragAxis.Both) d["dragAxis"] = dragOpt.Axis.ToString().ToLowerInvariant();
            d["dragElastic"] = dragOpt.Elastic;
            if (dragOpt.Constraints != null) d["dragConstraints"] = dragOpt.Constraints.ToJsObject();
            if (dragOpt.DirectionLock) d["dragDirectionLock"] = true;
        }
        return d;
    }

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
        PresenceCtx?.Unregister(this);
        Engine.UnregisterElement(_id);
        try { await Interop.UnregisterElementAsync(_id); } catch { /* ignore during teardown */ }
        try { await Interop.UnobserveViewportAsync(_id); } catch { /* ignore during teardown */ }
        _dotnet?.Dispose();
    }
}
