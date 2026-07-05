namespace Bit.Bmotion;

/// <summary>
/// Terse factories for the Bmotion hot path, designed to read like motion.dev inside Razor:
/// <code>
/// &lt;Bmotion Initial="Bm.To(opacity: 0, y: 20)"
///          Animate="Bm.To(opacity: 1, y: 0)"
///          Transition="Bm.Spring(stiffness: 200, damping: 20)"&gt;
///     &lt;div class="box" /&gt;
/// &lt;/Bmotion&gt;
/// </code>
/// Keyframe sequences are just another value shape:
/// <code>
/// Animate="Bm.To(scale: [1, 1.4, 0.8, 1], rotate: [0, 15, -10, 0])"
/// </code>
/// </summary>
public static class Bm
{
    /// <summary>
    /// Wildcard keyframe meaning "the element's current value", e.g. <c>x: [Bm.Current, 100]</c>.
    /// </summary>
    public const double Current = double.NaN;

    /// <summary>
    /// Creates an animation target. All parameters are optional; set only what should animate.
    /// The <c>transition</c> parameter embeds a transition in the target itself, overriding the
    /// component-level <c>Transition</c> when this target plays.
    /// </summary>
    public static BmProps To(
        // ── Transforms ──
        BmKeyframes? x = null, BmKeyframes? y = null, BmKeyframes? z = null,
        BmKeyframes? scale = null, BmKeyframes? scaleX = null, BmKeyframes? scaleY = null,
        BmKeyframes? rotate = null, BmKeyframes? rotateX = null, BmKeyframes? rotateY = null, BmKeyframes? rotateZ = null,
        BmKeyframes? skewX = null, BmKeyframes? skewY = null,
        BmKeyframes? perspective = null,
        double? originX = null, double? originY = null,
        // ── Visual ──
        BmKeyframes? opacity = null,
        BmStringKeyframes? backgroundColor = null, BmStringKeyframes? color = null,
        BmStringKeyframes? borderColor = null, BmStringKeyframes? outlineColor = null,
        BmStringKeyframes? fill = null, BmStringKeyframes? stroke = null,
        BmStringKeyframes? width = null, BmStringKeyframes? height = null,
        BmStringKeyframes? borderRadius = null, BmStringKeyframes? boxShadow = null,
        BmStringKeyframes? filter = null,
        // ── SVG path drawing ──
        BmKeyframes? pathLength = null, BmKeyframes? pathOffset = null, BmKeyframes? pathSpacing = null,
        // ── Extras ──
        Dictionary<string, string>? cssVars = null,
        BmTransition? transition = null)
        => new()
        {
            X = x, Y = y, Z = z,
            Scale = scale, ScaleX = scaleX, ScaleY = scaleY,
            Rotate = rotate, RotateX = rotateX, RotateY = rotateY, RotateZ = rotateZ,
            SkewX = skewX, SkewY = skewY,
            Perspective = perspective,
            OriginX = originX, OriginY = originY,
            Opacity = opacity,
            BackgroundColor = backgroundColor, Color = color,
            BorderColor = borderColor, OutlineColor = outlineColor,
            Fill = fill, Stroke = stroke,
            Width = width, Height = height,
            BorderRadius = borderRadius, BoxShadow = boxShadow,
            Filter = filter,
            PathLength = pathLength, PathOffset = pathOffset, PathSpacing = pathSpacing,
            CssVars = cssVars,
            Transition = transition,
        };

    /// <summary>
    /// A physics spring. Configure with <paramref name="stiffness"/>/<paramref name="damping"/>
    /// or, more intuitively, with <paramref name="bounce"/> (0-1) and <paramref name="duration"/>
    /// (visual seconds to reach the target).
    /// </summary>
    public static BmSpring Spring(
        double stiffness = 100, double damping = 10, double mass = 1,
        double? bounce = null, double? duration = null,
        double velocity = 0, double delay = 0, BmRepeat? repeat = null,
        double? staggerChildren = null, double? delayChildren = null)
        => new()
        {
            Stiffness = stiffness, Damping = damping, Mass = mass,
            Bounce = bounce, Duration = duration,
            Velocity = velocity, Delay = delay, Repeat = repeat,
            StaggerChildren = staggerChildren, DelayChildren = delayChildren,
        };

    /// <summary>A duration/easing tween.</summary>
    public static BmTween Tween(
        double duration = 0.3, BmEase ease = BmEase.Out,
        double delay = 0, BmRepeat? repeat = null,
        double[]? times = null, double[]? bezier = null, BmEase[]? eases = null,
        double? staggerChildren = null, double? delayChildren = null)
        => new()
        {
            Duration = duration, Ease = ease,
            Delay = delay, Repeat = repeat,
            Times = times, Bezier = bezier, Eases = eases,
            StaggerChildren = staggerChildren, DelayChildren = delayChildren,
        };

    /// <summary>
    /// A stagger delay generator for multi-element animations:
    /// <c>Motion.AnimateAsync(".item", Bm.To(y: 0), stagger: Bm.Stagger(0.08, from: BmStaggerFrom.Center))</c>.
    /// </summary>
    public static BmStagger Stagger(double each, BmStaggerFrom from = BmStaggerFrom.First, double startDelay = 0)
        => new(each, from, startDelay);

    /// <summary>
    /// Creates a reactive motion value (motion.dev's <c>motionValue()</c>) that can be bound to
    /// elements via the <c>Values</c> parameter, transformed, spring-followed and animated:
    /// <code>
    /// var x = Bm.Value(0.0);
    /// var angle = x.Transform([0, 200], [0, 360]);
    /// await Motion.AnimateAsync(x, 200, Bm.Spring());
    /// </code>
    /// </summary>
    public static BmValue<T> Value<T>(T initial)
        => new($"mv_{Guid.NewGuid():N}", initial);

    /// <summary>
    /// Composes motion values into a CSS string that re-renders whenever any input changes -
    /// motion.dev's <c>useMotionTemplate</c>. Bind the result to any CSS property via the
    /// component's <c>StringValues</c> parameter:
    /// <code>
    /// var blur = Bm.Value(0.0);
    /// var filter = Bm.Template(() => $"blur({blur.Value}px)", blur);
    /// // &lt;Bmotion StringValues='new() { ["filter"] = _filter }'&gt;
    /// </code>
    /// </summary>
    /// <param name="format">Produces the composed string; read the inputs' <c>Value</c> inside.</param>
    /// <param name="inputs">The motion values that trigger re-evaluation when they change.</param>
    public static BmValue<string> Template(Func<string> format, params BmValue<double>[] inputs)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(inputs);
        var composed = new BmValue<string>($"mv_{Guid.NewGuid():N}", format());
        foreach (var input in inputs)
            composed.AttachUpstream(input.Subscribe(_ => composed.SetSync(format())));
        return composed;
    }

    /// <summary>A momentum deceleration (used for drag release / fling effects).</summary>
    public static BmInertia Inertia(
        double velocity = 0, double timeConstant = 700, double power = 0.8,
        double? min = null, double? max = null, double delay = 0)
        => new()
        {
            Velocity = velocity, TimeConstant = timeConstant, Power = power,
            Min = min, Max = max, Delay = delay,
        };
}
