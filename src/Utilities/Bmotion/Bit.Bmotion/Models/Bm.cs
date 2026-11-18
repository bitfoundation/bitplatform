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
    public static BmotionProps To(
        // ── Transforms ──
        BmotionKeyframes? x = null, BmotionKeyframes? y = null, BmotionKeyframes? z = null,
        BmotionKeyframes? scale = null, BmotionKeyframes? scaleX = null, BmotionKeyframes? scaleY = null,
        BmotionKeyframes? rotate = null, BmotionKeyframes? rotateX = null, BmotionKeyframes? rotateY = null, BmotionKeyframes? rotateZ = null,
        BmotionKeyframes? skewX = null, BmotionKeyframes? skewY = null,
        BmotionKeyframes? perspective = null,
        double? originX = null, double? originY = null,
        // ── Visual ──
        BmotionKeyframes? opacity = null,
        BmotionStringKeyframes? backgroundColor = null, BmotionStringKeyframes? color = null,
        BmotionStringKeyframes? borderColor = null, BmotionStringKeyframes? outlineColor = null,
        BmotionStringKeyframes? fill = null, BmotionStringKeyframes? stroke = null,
        BmotionStringKeyframes? width = null, BmotionStringKeyframes? height = null,
        BmotionStringKeyframes? borderRadius = null, BmotionStringKeyframes? boxShadow = null,
        BmotionStringKeyframes? filter = null,
        // ── Layout / box-model ──
        BmotionStringKeyframes? top = null, BmotionStringKeyframes? left = null,
        BmotionStringKeyframes? right = null, BmotionStringKeyframes? bottom = null,
        BmotionStringKeyframes? margin = null, BmotionStringKeyframes? padding = null,
        BmotionStringKeyframes? gap = null,
        // ── Typography ──
        BmotionStringKeyframes? letterSpacing = null, BmotionStringKeyframes? lineHeight = null,
        BmotionStringKeyframes? fontSize = null,
        // ── Misc CSS ──
        BmotionStringKeyframes? clipPath = null,
        BmotionStringKeyframes? backgroundPosition = null, BmotionStringKeyframes? backgroundSize = null,
        // ── Motion path ──
        BmotionStringKeyframes? offsetPath = null, BmotionStringKeyframes? offsetDistance = null,
        // ── SVG shape morph ──
        BmotionStringKeyframes? d = null,
        // ── SVG path drawing ──
        BmotionKeyframes? pathLength = null, BmotionKeyframes? pathOffset = null, BmotionKeyframes? pathSpacing = null,
        // ── Extras ──
        Dictionary<string, string>? cssVars = null,
        Dictionary<string, BmotionStringKeyframes>? css = null,
        BmotionTransition? transition = null)
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
            Top = top, Left = left, Right = right, Bottom = bottom,
            Margin = margin, Padding = padding, Gap = gap,
            LetterSpacing = letterSpacing, LineHeight = lineHeight, FontSize = fontSize,
            ClipPath = clipPath, BackgroundPosition = backgroundPosition, BackgroundSize = backgroundSize,
            OffsetPath = offsetPath, OffsetDistance = offsetDistance,
            D = d,
            PathLength = pathLength, PathOffset = pathOffset, PathSpacing = pathSpacing,
            CssVars = cssVars,
            Css = css,
            Transition = transition,
        };

    /// <summary>
    /// A physics spring. Configure with <paramref name="stiffness"/>/<paramref name="damping"/>
    /// or, more intuitively, with <paramref name="bounce"/> (0-1) and <paramref name="duration"/>
    /// (visual seconds to reach the target).
    /// </summary>
    public static BmotionSpring Spring(
        double stiffness = 100, double damping = 10, double mass = 1,
        double? bounce = null, double? duration = null,
        double velocity = 0, double delay = 0, BmotionRepeat? repeat = null,
        BmotionColorSpace? colorSpace = null,
        double? staggerChildren = null, double? delayChildren = null,
        BmotionWhen when = BmotionWhen.Together, BmotionStagger? childStagger = null, BmotionArc? path = null)
        => new()
        {
            Stiffness = stiffness, Damping = damping, Mass = mass,
            Bounce = bounce, Duration = duration,
            Velocity = velocity, Delay = delay, Repeat = repeat,
            ColorSpace = colorSpace,
            StaggerChildren = staggerChildren, DelayChildren = delayChildren, When = when, ChildStagger = childStagger, Path = path,
        };

    /// <summary>
    /// Bends the straight line an element travels between two points into a curve - motion.dev's
    /// <c>arc()</c>. Pass it to a transition's <c>path</c> and animate <c>x</c> and <c>y</c>
    /// together:
    /// <code>
    /// Transition="Bm.Tween(0.8, path: Bm.Arc(strength: 0.8, rotate: 1))"
    /// </code>
    /// </summary>
    /// <param name="strength">How far the curve bends, as a fraction of the distance travelled (0-1).</param>
    /// <param name="peak">Where along the journey the bend is centred (0-1).</param>
    /// <param name="direction">Which side the arc bulges towards.</param>
    /// <param name="rotate">How much the element turns to follow the curve (0 = upright, 1 = along the tangent).</param>
    public static BmotionArc Arc(
        double strength = 0.5, double peak = 0.5,
        BmotionArcDirection direction = BmotionArcDirection.Auto, double rotate = 0)
        => new() { Strength = strength, Peak = peak, Direction = direction, Rotate = rotate };

    /// <summary>A duration/easing tween.</summary>
    public static BmotionTween Tween(
        double duration = 0.3, BmotionEase ease = BmotionEase.Out,
        double delay = 0, BmotionRepeat? repeat = null,
        double[]? times = null, double[]? bezier = null, BmotionEase[]? eases = null,
        int steps = 0, BmotionStepJump stepJump = BmotionStepJump.End,
        BmotionColorSpace? colorSpace = null,
        double? staggerChildren = null, double? delayChildren = null,
        BmotionWhen when = BmotionWhen.Together, BmotionStagger? childStagger = null, BmotionArc? path = null)
        => new()
        {
            Duration = duration, Ease = ease,
            Delay = delay, Repeat = repeat,
            Times = times, Bezier = bezier, Eases = eases,
            Steps = steps, StepJump = stepJump,
            ColorSpace = colorSpace,
            StaggerChildren = staggerChildren, DelayChildren = delayChildren, When = when, ChildStagger = childStagger, Path = path,
        };

    /// <summary>
    /// A stagger delay generator for multi-element animations:
    /// <c>Motion.AnimateAsync(".item", Bm.To(y: 0), stagger: Bm.Stagger(0.08, from: BmotionStaggerFrom.Center))</c>.
    /// </summary>
    public static BmotionStagger Stagger(double each, BmotionStaggerFrom from = BmotionStaggerFrom.First, double startDelay = 0,
        (int Cols, int Rows)? grid = null)
        => new(each, from, startDelay, grid);

    /// <summary>
    /// A stagger driven entirely by a custom <c>(index, total) =&gt; delaySeconds</c> function:
    /// <c>Bm.Stagger((i, total) =&gt; i * 0.05)</c>.
    /// </summary>
    public static BmotionStagger Stagger(Func<int, int, double> custom)
        => new(custom);

    /// <summary>
    /// Creates a reactive motion value (motion.dev's <c>motionValue()</c>) that can be bound to
    /// elements via the <c>Values</c> parameter, transformed, spring-followed and animated:
    /// <code>
    /// var x = Bm.Value(0.0);
    /// var angle = x.Transform([0, 200], [0, 360]);
    /// await Motion.AnimateAsync(x, 200, Bm.Spring());
    /// </code>
    /// </summary>
    public static BmotionValue<T> Value<T>(T initial)
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
    public static BmotionValue<string> Template(Func<string> format, params BmotionValue<double>[] inputs)
    {
        ArgumentNullException.ThrowIfNull(format);
        ThrowIfAnyNull(inputs);
        var composed = new BmotionValue<string>($"mv_{Guid.NewGuid():N}", format());
        foreach (var input in inputs)
            composed.AttachUpstream(SubscribeWeak(input, composed, format));
        return composed;
    }

    /// <summary>
    /// A numeric motion value computed from several others, recomputed whenever any of them changes - motion.dev's
    /// <c>useTransform(() =&gt; ...)</c> form. It tracks its own velocity, so it can feed a spring follower, another
    /// range map or a binding like any value:
    /// <code>
    /// var x = Bm.Value(0.0);
    /// var y = Bm.Value(0.0);
    /// var distance = Bm.Combine(() =&gt; Math.Sqrt(x.Value * x.Value + y.Value * y.Value), x, y);
    /// var glow = distance.Transform([0, 300], [0, 1]);
    /// </code>
    /// A change of one input recomputes it once; setting several inputs in a row recomputes it once per change. Dispose
    /// it to detach it from its inputs.
    /// </summary>
    /// <param name="compute">Produces the value; read the inputs' <c>Value</c> inside.</param>
    /// <param name="inputs">The motion values that trigger a recomputation when they change.</param>
    public static BmotionValue<double> Combine(Func<double> compute, params BmotionValue<double>[] inputs)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ThrowIfAnyNull(inputs);
        var combined = new BmotionValue<double>($"mv_{Guid.NewGuid():N}", compute());
        foreach (var input in inputs)
            combined.AttachUpstream(SubscribeWeak(input, combined, compute));
        return combined;
    }

    // Every input is checked before the first is subscribed to: a null one found half-way would leave the inputs
    // before it subscribed to a composite the caller never received, recomputing it on each of their changes.
    private static void ThrowIfAnyNull(BmotionValue<double>[] inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        foreach (var input in inputs)
            ArgumentNullException.ThrowIfNull(input, nameof(inputs));
    }

    /// <summary>
    /// Recomputes <paramref name="derived"/> on every change of <paramref name="input"/> through a weak reference, as
    /// <see cref="BmotionValue{T}.Transform{TOut}(Func{T, TOut})"/> does: the input never keeps the derived value alive, so a
    /// composite that is dropped without <c>Dispose()</c> is collected instead of leaking (and recomputing) for as long
    /// as its inputs live. The subscription removes itself the first time it fires after the derived value is gone.
    /// </summary>
    private static IDisposable SubscribeWeak<TOut>(BmotionValue<double> input, BmotionValue<TOut> derived, Func<TOut> compute)
    {
        var weak = new WeakReference<BmotionValue<TOut>>(derived);
        IDisposable? sub = null;
        sub = input.Subscribe(_ =>
        {
            if (weak.TryGetTarget(out var target))
                target.SetSync(compute());
            else
                sub?.Dispose();
        });
        return sub;
    }

    /// <summary>
    /// A derived motion value carrying <paramref name="source"/>'s velocity in units per second -
    /// motion.dev's <c>useVelocity</c>. Feed it into a range map to drive skew-on-scroll,
    /// speed-reactive blur and similar effects:
    /// <code>
    /// var velocity = Bm.Velocity(Scroll.ScrollYValue);
    /// var skew = velocity.Transform([-2000, 0, 2000], [-8, 0, 8]);
    /// </code>
    /// It returns to 0 shortly after the source stops changing - within a few of the source's own update
    /// intervals - so an effect driven by it comes back to rest when the scroll ends. Follow it with
    /// <see cref="BmotionAnimateService.Spring"/> to ease that return instead of landing on it. That return
    /// runs on the thread the source's last change came from, so it needs a source changed from the
    /// renderer's (as the scroll tracker's and a component's own changes are): one changed from a background
    /// thread keeps its last speed until the next change. Dispose the value when done with it.
    /// </summary>
    public static BmotionValue<double> Velocity(BmotionValue<double> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // The source tracks velocity as part of every set, so the derived value only has to read it
        // back after each change - no separate frame loop, and it works on Blazor Server too. A source that
        // stops sends no further change, though, so the settle brings the velocity back to 0 once it is still.
        var velocity = source.Transform(_ => source.GetVelocity());
        velocity.AttachUpstream(new BmotionVelocitySettle(source, velocity));
        return velocity;
    }

    /// <summary>Constrains <paramref name="value"/> to the inclusive <c>[min, max]</c> range.</summary>
    public static double Clamp(double min, double max, double value)
        => value < min ? min : value > max ? max : value;

    /// <summary>
    /// Wraps <paramref name="value"/> into the half-open range <c>[min, max)</c>, so it loops round
    /// instead of clamping - motion.dev's <c>wrap</c>. Useful for infinite marquees and carousels.
    /// </summary>
    public static double Wrap(double min, double max, double value)
    {
        double range = max - min;
        if (range == 0) return min;
        // The double modulo keeps negative inputs inside the range instead of mirroring them.
        return ((value - min) % range + range) % range + min;
    }

    /// <summary>Linearly interpolates from <paramref name="from"/> to <paramref name="to"/> at <paramref name="progress"/> (0-1).</summary>
    public static double Mix(double from, double to, double progress)
        => from + (to - from) * progress;

    /// <summary>
    /// One-shot version of <see cref="BmotionValue{T}.Transform(double[], double[], bool)"/>: maps
    /// <paramref name="value"/> from <paramref name="inputRange"/> onto <paramref name="outputRange"/>
    /// with linear interpolation between the points - motion.dev's <c>transform</c>.
    /// </summary>
    /// <param name="value">The input value to map.</param>
    /// <param name="inputRange">Strictly increasing input points.</param>
    /// <param name="outputRange">The output point for each input point.</param>
    /// <param name="clamp">When <c>true</c> (default), values outside the input range clamp to the
    /// output ends; when <c>false</c>, the outermost segments extrapolate.</param>
    public static double MapRange(double value, double[] inputRange, double[] outputRange, bool clamp = true)
    {
        ArgumentNullException.ThrowIfNull(inputRange);
        ArgumentNullException.ThrowIfNull(outputRange);
        return BmotionRangeMap.Map(value, inputRange, outputRange, clamp);
    }

    /// <summary>
    /// A momentum deceleration (used for drag release / fling effects). Pass
    /// <paramref name="modifyTarget"/> to snap where the coast comes to rest - a grid, a page, a set
    /// of stops: <c>Bm.Inertia(modifyTarget: v =&gt; Math.Round(v / 100) * 100)</c>.
    /// </summary>
    public static BmotionInertia Inertia(
        double velocity = 0, double timeConstant = 700, double power = 0.8,
        double? min = null, double? max = null, double delay = 0,
        Func<double, double>? modifyTarget = null)
        => new()
        {
            Velocity = velocity, TimeConstant = timeConstant, Power = power,
            Min = min, Max = max, Delay = delay, ModifyTarget = modifyTarget,
        };

    /// <summary>
    /// A <see cref="Func{T, TResult}"/> that rounds a value to the nearest multiple of
    /// <paramref name="step"/> - the snap-to-grid hook for
    /// <see cref="Inertia"/>/<c>DragTransition</c>:
    /// <code>
    /// &lt;Bmotion Drag="BmotionDrag.X" DragTransition="Bm.Inertia(modifyTarget: Bm.SnapTo(120))"&gt;
    /// </code>
    /// </summary>
    /// <param name="step">The grid interval; must be finite and greater than zero.</param>
    /// <param name="origin">The value the grid is anchored on. Default 0.</param>
    public static Func<double, double> SnapTo(double step, double origin = 0)
    {
        if (!double.IsFinite(step) || step <= 0)
            throw new ArgumentException("Snap step must be a finite number greater than zero.", nameof(step));
        if (!double.IsFinite(origin))
            throw new ArgumentException("Snap origin must be finite.", nameof(origin));
        return v => origin + Math.Round((v - origin) / step, MidpointRounding.AwayFromZero) * step;
    }

    /// <summary>
    /// A <see cref="Func{T, TResult}"/> that snaps a value to the nearest of an explicit set of
    /// stops - the carousel/page-snap hook for <see cref="Inertia"/>/<c>DragTransition</c>:
    /// <code>
    /// DragTransition="Bm.Inertia(modifyTarget: Bm.SnapTo([0, -320, -640]))"
    /// </code>
    /// </summary>
    /// <param name="stops">The candidate resting values; must be non-empty and finite.</param>
    // Deliberately not `params`: with it, Bm.SnapTo(120, 0) would be ambiguous with the
    // (step, origin) overload above and silently resolve to the wrong one. Requiring the collection
    // expression keeps the two readings visibly distinct at the call site.
    public static Func<double, double> SnapTo(double[] stops)
    {
        ArgumentNullException.ThrowIfNull(stops);
        if (stops.Length == 0)
            throw new ArgumentException("At least one snap stop is required.", nameof(stops));
        if (!Array.TrueForAll(stops, double.IsFinite))
            throw new ArgumentException("Snap stops must all be finite.", nameof(stops));
        // Copy so a later mutation of the caller's array can't change where an in-flight fling lands.
        var candidates = (double[])stops.Clone();
        return v =>
        {
            double best = candidates[0];
            double bestDistance = Math.Abs(v - best);
            for (int i = 1; i < candidates.Length; i++)
            {
                double distance = Math.Abs(v - candidates[i]);
                if (distance < bestDistance) { best = candidates[i]; bestDistance = distance; }
            }
            return best;
        };
    }
}
