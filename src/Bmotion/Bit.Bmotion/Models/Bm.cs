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
        // ── Layout / box-model ──
        BmStringKeyframes? top = null, BmStringKeyframes? left = null,
        BmStringKeyframes? right = null, BmStringKeyframes? bottom = null,
        BmStringKeyframes? margin = null, BmStringKeyframes? padding = null,
        BmStringKeyframes? gap = null,
        // ── Typography ──
        BmStringKeyframes? letterSpacing = null, BmStringKeyframes? lineHeight = null,
        BmStringKeyframes? fontSize = null,
        // ── Misc CSS ──
        BmStringKeyframes? clipPath = null,
        BmStringKeyframes? backgroundPosition = null, BmStringKeyframes? backgroundSize = null,
        // ── Motion path ──
        BmStringKeyframes? offsetPath = null, BmStringKeyframes? offsetDistance = null,
        // ── SVG shape morph ──
        BmStringKeyframes? d = null,
        // ── SVG path drawing ──
        BmKeyframes? pathLength = null, BmKeyframes? pathOffset = null, BmKeyframes? pathSpacing = null,
        // ── Extras ──
        Dictionary<string, string>? cssVars = null,
        Dictionary<string, BmStringKeyframes>? css = null,
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
    public static BmSpring Spring(
        double stiffness = 100, double damping = 10, double mass = 1,
        double? bounce = null, double? duration = null,
        double velocity = 0, double delay = 0, BmRepeat? repeat = null,
        BmColorSpace? colorSpace = null,
        double? staggerChildren = null, double? delayChildren = null,
        BmWhen when = BmWhen.Together, BmStagger? childStagger = null, BmArc? path = null)
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
    public static BmArc Arc(
        double strength = 0.5, double peak = 0.5,
        BmArcDirection direction = BmArcDirection.Auto, double rotate = 0)
        => new() { Strength = strength, Peak = peak, Direction = direction, Rotate = rotate };

    /// <summary>A duration/easing tween.</summary>
    public static BmTween Tween(
        double duration = 0.3, BmEase ease = BmEase.Out,
        double delay = 0, BmRepeat? repeat = null,
        double[]? times = null, double[]? bezier = null, BmEase[]? eases = null,
        int steps = 0, BmStepJump stepJump = BmStepJump.End,
        BmColorSpace? colorSpace = null,
        double? staggerChildren = null, double? delayChildren = null,
        BmWhen when = BmWhen.Together, BmStagger? childStagger = null, BmArc? path = null)
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
    /// <c>Motion.AnimateAsync(".item", Bm.To(y: 0), stagger: Bm.Stagger(0.08, from: BmStaggerFrom.Center))</c>.
    /// </summary>
    public static BmStagger Stagger(double each, BmStaggerFrom from = BmStaggerFrom.First, double startDelay = 0,
        (int Cols, int Rows)? grid = null)
        => new(each, from, startDelay, grid);

    /// <summary>
    /// A stagger driven entirely by a custom <c>(index, total) =&gt; delaySeconds</c> function:
    /// <c>Bm.Stagger((i, total) =&gt; i * 0.05)</c>.
    /// </summary>
    public static BmStagger Stagger(Func<int, int, double> custom)
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

    /// <summary>
    /// A derived motion value carrying <paramref name="source"/>'s velocity in units per second -
    /// motion.dev's <c>useVelocity</c>. Feed it into a range map to drive skew-on-scroll,
    /// speed-reactive blur and similar effects:
    /// <code>
    /// var velocity = Bm.Velocity(Scroll.ScrollYValue);
    /// var skew = velocity.Transform([-2000, 0, 2000], [-8, 0, 8]);
    /// </code>
    /// </summary>
    public static BmValue<double> Velocity(BmValue<double> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // The source tracks velocity as part of every set, so the derived value only has to read it
        // back after each change - no separate frame loop, and it works on Blazor Server too.
        return source.Transform(_ => source.GetVelocity());
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
    /// One-shot version of <see cref="BmValue{T}.Transform(double[], double[], bool)"/>: maps
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
        return BmRangeMap.Map(value, inputRange, outputRange, clamp);
    }

    /// <summary>
    /// A momentum deceleration (used for drag release / fling effects). Pass
    /// <paramref name="modifyTarget"/> to snap where the coast comes to rest - a grid, a page, a set
    /// of stops: <c>Bm.Inertia(modifyTarget: v =&gt; Math.Round(v / 100) * 100)</c>.
    /// </summary>
    public static BmInertia Inertia(
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
    /// &lt;Bmotion Drag="BmDrag.X" DragTransition="Bm.Inertia(modifyTarget: Bm.SnapTo(120))"&gt;
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
