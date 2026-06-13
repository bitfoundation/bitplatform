using Bit.Bmotion.Context;
using Bit.Bmotion.Engine;
using Bit.Bmotion.Interop;
using Bit.Bmotion.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Bit.Bmotion.Components;

/// <summary>
/// The primary animation component — a drop-in replacement for any HTML element.
/// Animation math runs in the C# <see cref="AnimationEngine"/>; JS is used only
/// for DOM style mutation, pointer/focus events, viewport observation and FLIP.
/// </summary>
public class Motion : ComponentBase, IAsyncDisposable
{
    // ── Injected services ──────────────────────────────────────────────────────
    [Inject] private AnimationEngine Engine { get; set; } = null!;
    [Inject] private MotionInterop Interop { get; set; } = null!;

    // ── Cascaded contexts ──────────────────────────────────────────────────────
    [CascadingParameter] private PresenceContext? PresenceCtx { get; set; }
    [CascadingParameter] private VariantContext? VariantCtx { get; set; }
    [CascadingParameter] private MotionConfigContext? ConfigCtx { get; set; }

    // ── Core rendering parameters ────────────────────────────────────────────
    [Parameter] public string Tag { get; set; } = "div";
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Animation targets ──────────────────────────────────────────────────────
    [Parameter] public AnimationTarget? Initial { get; set; }
    [Parameter] public AnimationTarget? Animate { get; set; }
    [Parameter] public AnimationTarget? Exit { get; set; }

    // ── Gesture states ─────────────────────────────────────────────────────────
    [Parameter] public AnimationTarget? WhileHover { get; set; }
    [Parameter] public AnimationTarget? WhileTap { get; set; }
    [Parameter] public AnimationTarget? WhileFocus { get; set; }
    [Parameter] public AnimationTarget? WhileDrag { get; set; }
    [Parameter] public AnimationTarget? WhileInView { get; set; }

    /// <summary>
    /// If <c>true</c>, <see cref="WhileInView"/> fires only once and never deactivates.
    /// Shorthand for <c>Viewport = new ViewportOptions { Once = true }</c>.
    /// </summary>
    [Parameter] public bool Once { get; set; }

    /// <summary>
    /// Advanced viewport options for <see cref="WhileInView"/> (margin, amount, once).
    /// When set, <see cref="Once"/> is ignored in favour of <c>Viewport.Once</c>.
    /// </summary>
    [Parameter] public ViewportOptions? Viewport { get; set; }

    // ── Transition ─────────────────────────────────────────────────────────────
    [Parameter] public TransitionConfig? Transition { get; set; }

    // ── Variants ─────────────────────────────────────────────────────────────
    [Parameter] public MotionVariants? Variants { get; set; }

    // ── Drag ─────────────────────────────────────────────────────────────────
    [Parameter] public bool Drag { get; set; }
    [Parameter] public DragOptions? DragOptions { get; set; }

    // ── Layout ─────────────────────────────────────────────────────────────────
    [Parameter] public bool Layout { get; set; }
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
    [Parameter] public EventCallback<PanInfo> OnPan { get; set; }
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
    private DotNetObjectReference<Motion>? _dotnet;
    private bool _initialized;
    private bool _isExiting;
    private AnimationTarget? _prevAnimate;
    private VariantContext? _ownVariantCtx;
    private string? _prevInheritedVariant;
    private int _variantChildIndex = -1;
    private BoundingRect? _layoutSnapshot;

    // ════════════════════════════════════════════════════════════════════════════
    // Rendering
    // ════════════════════════════════════════════════════════════════════════════

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        int seq = 0;
        builder.OpenElement(seq++, Tag);
        builder.AddAttribute(seq++, "id", _id);

        if (AdditionalAttributes != null)
            builder.AddMultipleAttributes(seq++, AdditionalAttributes);

        // Auto-inject pathLength="1" so normalized [0,1] dasharray coordinates work correctly
        if (Tag == "path" && NeedsPathLengthAttr())
            builder.AddAttribute(seq++, "pathLength", "1");

        if (!string.IsNullOrEmpty(Class))
            builder.AddAttribute(seq++, "class", Class);

        var motionStyle = BuildInitialStyle();
        var combinedStyle = string.IsNullOrEmpty(Style) ? motionStyle : motionStyle + Style;
        if (!string.IsNullOrEmpty(combinedStyle))
            builder.AddAttribute(seq++, "style", combinedStyle);

        builder.AddElementReferenceCapture(seq++, r => _ref = r);

        if (Variants != null)
        {
            _ownVariantCtx ??= new VariantContext();
            _ownVariantCtx.ActiveVariant = Animate?.IsVariant == true ? Animate.Variant : null;
            _ownVariantCtx.InitialVariant = Initial?.IsVariant == true ? Initial.Variant : null;
            _ownVariantCtx.Variants = Variants;
            _ownVariantCtx.StaggerChildren = Transition?.StaggerChildren ?? 0;
            _ownVariantCtx.DelayChildren = Transition?.DelayChildren ?? 0;

            builder.OpenComponent<CascadingValue<VariantContext>>(seq++);
            builder.AddComponentParameter(seq++, "Value", _ownVariantCtx);
            builder.AddComponentParameter(seq++, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(seq++, ChildContent);
        }
        builder.CloseElement();
    }

    private string BuildInitialStyle()
    {
        var props = ResolveProps(Initial);
        if (props == null && Animate == null && VariantCtx?.InitialVariant is string initVariant)
            props = Variants?.Get(initVariant) ?? VariantCtx.Variants?.Get(initVariant);
        return props?.ToCssStyleString() ?? string.Empty;
    }

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
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (PresenceCtx is { IsExiting: true } && !_isExiting)
        {
            _isExiting = true;
            if (_initialized) await PlayExitAsync();
        }

        // FLIP: snapshot BEFORE re-render
        if (_initialized && Layout && !_isExiting)
            _layoutSnapshot = await Interop.GetBoundingRectAsync(_id);
    }

    private async Task InitialiseAsync()
    {
        // Reduced-motion is opt-in: only probe the OS preference when this element is
        // inside a <MotionConfig>. Elements without a config always animate normally.
        if (ConfigCtx is not null)
            await Engine.EnsureReducedMotionDetectedAsync();

        // Register with C# engine (applies initial values synchronously)
        var initProps = ResolveProps(Initial);
        Engine.RegisterElement(_id, initProps?.ToJsDictionary());

        // Mark element in the DOM for JS bridge
        await Interop.RegisterElementAsync(_id);

        PresenceCtx?.Register(this);

        // Attach events the JS bridge needs to listen to
        var events = BuildEventFlags();
        if (events.Count > 0)
            await Interop.AttachEventListenersAsync(_id, events, _dotnet!);

        // Viewport observation — JS IntersectionObserver callbacks C#
        if (WhileInView != null || OnViewportEnter.HasDelegate || OnViewportLeave.HasDelegate)
        {
            if (Viewport != null)
                await Interop.ObserveViewportWithOptionsAsync(_id, _dotnet!, Viewport);
            else
                await Interop.ObserveViewportAsync(_id, _dotnet!, Once);
        }

        // Start enter animation
        if (Animate != null)
        {
            var animateProps = ResolveProps(Animate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync();
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(),
                    () => OnAnimationComplete.InvokeAsync());
            }
        }
        else if (VariantCtx?.ActiveVariant is string inheritedVariant && Variants != null)
        {
            _variantChildIndex = VariantCtx.RegisterChild();
            _prevInheritedVariant = inheritedVariant;
            var props = Variants.Get(inheritedVariant) ?? VariantCtx.Variants?.Get(inheritedVariant);
            if (props != null)
                await Engine.AnimateToAsync(_id, props.ToJsDictionary(),
                    BuildEffectiveTransitionWithDelay(VariantCtx.GetChildDelay(_variantChildIndex)));
        }

        _prevAnimate = Animate;
    }

    private async Task HandleParameterUpdateAsync()
    {
        if (_isExiting) return;

        if (!ReferenceEquals(_prevAnimate, Animate))
        {
            var animateProps = ResolveProps(Animate);
            if (animateProps != null)
            {
                await OnAnimationStart.InvokeAsync();
                await Engine.AnimateToAsync(_id, animateProps.ToJsDictionary(), BuildEffectiveTransition(),
                    () => OnAnimationComplete.InvokeAsync());
            }
            _prevAnimate = Animate;
        }
        else if (Animate == null && Variants != null)
        {
            var newVariant = VariantCtx?.ActiveVariant;
            if (newVariant != _prevInheritedVariant)
            {
                _prevInheritedVariant = newVariant;
                if (newVariant != null)
                {
                    var props = Variants.Get(newVariant) ?? VariantCtx?.Variants?.Get(newVariant);
                    if (props != null)
                    {
                        double delay = _variantChildIndex >= 0 ? VariantCtx!.GetChildDelay(_variantChildIndex) : 0;
                        await Engine.AnimateToAsync(_id, props.ToJsDictionary(),
                            BuildEffectiveTransitionWithDelay(delay));
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

    private async Task PlayFlipAsync(BoundingRect snap)
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
        double dur = t?.Type == TransitionType.Spring ? 600 : (t?.Duration ?? 0.5) * 1000;
        string easing = t?.Type == TransitionType.Spring
            ? "cubic-bezier(0.14,1,0.34,1)"
            : EasingFunctions.ToCssString(t);
        string? finalT = Engine.GetCurrentTransformString(_id);

        await Interop.PlayWaapiFlipAsync(_id, dx, dy, sx, sy, dur, easing, finalT);
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Programmatic API
    // ════════════════════════════════════════════════════════════════════════════

    public async ValueTask AnimateAsync(AnimationProps props, TransitionConfig? transition = null)
    {
        transition ??= BuildEffectiveTransition();
        await Engine.AnimateToAsync(_id, props.ToJsDictionary(), transition);
    }

    public void Set(AnimationProps props) => Engine.SetInstant(_id, props.ToJsDictionary());

    public async ValueTask SetAsync(AnimationProps props)
    {
        Engine.SetInstant(_id, props.ToJsDictionary());
        // Flush synchronous style update to DOM
        var styles = BuildCssStyleDict(props);
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
        if (WhileHover != null)
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
        if (WhileTap != null)
            await Engine.DeactivateGestureLayerAsync(_id, "tap");
        if (isInsideElement) await OnTap.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnPointerCancel()
    {
        if (WhileTap != null)
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
        if (WhileFocus != null)
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
    public object GetCurrentXY()
    {
        var (x, y) = Engine.GetCurrentXY(_id);
        return new { x, y };
    }

    [JSInvokable] public async Task OnDragMove() => await OnDrag.InvokeAsync();

    [JSInvokable]
    public async Task OnPointerUp_Drag(double velX, double velY)
    {
        if (WhileDrag != null)
            await Engine.DeactivateGestureLayerAsync(_id, "drag");

        var dragOpt = DragOptions ?? new DragOptions();

        if (dragOpt.SnapToOrigin)
        {
            var snapT = dragOpt.SnapTransition ?? new TransitionConfig
                { Type = TransitionType.Spring, Stiffness = 400, Damping = 35 };
            await Engine.AnimateToAsync(_id,
                new Dictionary<string, object?> { ["x"] = 0.0, ["y"] = 0.0 }, snapT);
        }
        else
        {
            await Engine.EndDragAsync(
                _id, velX, velY, dragOpt.Momentum, dragOpt.Constraints,
                dragOpt.Axis == DragAxis.Both ? null : dragOpt.Axis.ToString().ToLowerInvariant(),
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
            await OnPan.InvokeAsync(new PanInfo
            {
                Point    = new PointInfo { X = pointX,    Y = pointY },
                Delta    = new PointInfo { X = deltaX,    Y = deltaY },
                Offset   = new PointInfo { X = offsetX,   Y = offsetY },
                Velocity = new PointInfo { X = velocityX, Y = velocityY },
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
            if (WhileInView != null && !Once)
                await Engine.DeactivateGestureLayerAsync(_id, "inview");
            await OnViewportLeave.InvokeAsync();
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════════

    private bool NeedsPathLengthAttr() =>
        (AdditionalAttributes == null || !AdditionalAttributes.ContainsKey("pathLength")) &&
        (HasPathLength(Initial) || HasPathLength(Animate) || HasPathLength(Exit) ||
         HasPathLength(WhileHover) || HasPathLength(WhileTap) || HasPathLength(WhileFocus) ||
         HasPathLength(WhileInView) || HasPathLength(WhileDrag));

    private static bool HasPathLength(AnimationTarget? t) =>
        t?.Props?.PathLength != null;

    private AnimationProps? ResolveProps(AnimationTarget? target)
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
    /// <see cref="Components.MotionConfig"/>. Within one, an explicit
    /// <see cref="MotionConfigContext.ReduceMotion"/> value (true/false) always wins; when it is
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
    private static TransitionConfig InstantTransition()
        => new() { Type = TransitionType.Tween, Duration = 0, Delay = 0 };

    private TransitionConfig? BuildEffectiveTransition()
    {
        // Reduced motion: collapse every animation to an instant state change.
        if (ShouldReduceMotion()) return InstantTransition();

        var t = Transition ?? ConfigCtx?.DefaultTransition;
        if (t == null) return null;
        if (ConfigCtx?.TransitionSpeed is double speed && speed != 1.0)
        {
            t = t.Clone();
            t.Duration *= speed;
        }
        return t;
    }

    private TransitionConfig BuildEffectiveTransitionWithDelay(double extraDelay)
    {
        // Reduced motion stays instant — stagger delays are skipped too.
        if (ShouldReduceMotion()) return InstantTransition();

        var t = BuildEffectiveTransition() ?? new TransitionConfig();
        if (extraDelay <= 0) return t;
        t = t.Clone();
        t.Delay += extraDelay;
        return t;
    }

    private Dictionary<string, object?> BuildEventFlags()
    {
        var d = new Dictionary<string, object?>();
        if (WhileHover != null || OnHoverStart.HasDelegate || OnHoverEnd.HasDelegate) d["hover"] = true;
        if (WhileTap != null || OnTapStart.HasDelegate || OnTap.HasDelegate) d["tap"] = true;
        if (WhileFocus != null || OnFocusStart.HasDelegate || OnFocusEnd.HasDelegate) d["focus"] = true;
        if (OnPanStart.HasDelegate || OnPan.HasDelegate || OnPanEnd.HasDelegate) d["pan"] = true;
        if (Drag)
        {
            d["drag"] = true;
            var dragOpt = DragOptions ?? new DragOptions();
            if (dragOpt.Axis != DragAxis.Both) d["dragAxis"] = dragOpt.Axis.ToString().ToLowerInvariant();
            d["dragElastic"] = dragOpt.Elastic;
            if (dragOpt.Constraints != null) d["dragConstraints"] = dragOpt.Constraints.ToJsObject();
            if (dragOpt.DirectionLock) d["dragDirectionLock"] = true;
        }
        return d;
    }

    private static Dictionary<string, string> BuildCssStyleDict(AnimationProps props)
    {
        var d = new Dictionary<string, string>();
        // This is only used for instant set() — forward the CSS string parsed from props
        var css = props.ToCssStyleString();
        if (!string.IsNullOrEmpty(css))
            d["cssText"] = css; // handled on JS side by parsing cssText
        return d;
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
