namespace Bit.Bmotion;

/// <summary>A side of an element or of the viewport: where something enters from, or which way it points.</summary>
public enum BmEdge
{
    Top,
    Right,
    Bottom,
    Left,
}

/// <summary>An axis in the plane of the screen.</summary>
public enum BmAxis
{
    X,
    Y,
}

/// <summary>
/// Ready-made <see cref="BmPreset"/>s for the motion most interfaces need - entrances and exits, overlays, hover and
/// press feedback, attention effects, scroll reveals and loading indicators. Each is a starting point, not a fixed
/// look: every factory takes the knobs that matter, presets compose with <c>+</c>, and anything set on the
/// <c>&lt;Bmotion&gt;</c> itself wins over the preset.
/// <code>
/// &lt;BmotionAnimatePresence IsPresent="_open"&gt;
///     &lt;Bmotion Preset="BmPresets.Modal()"&gt;&lt;div class="dialog" role="dialog"&gt;...&lt;/div&gt;&lt;/Bmotion&gt;
/// &lt;/BmotionAnimatePresence&gt;
///
/// &lt;Bmotion Preset="BmPresets.Fade() + BmPresets.HoverLift()"&gt;&lt;div class="card" /&gt;&lt;/Bmotion&gt;
/// </code>
/// <para>
/// Reduced motion applies to presets like to any animation: under it, movement, scaling and rotation land at once
/// while fades, colours and filters keep their timing - so an entrance becomes a fade and an attention shake does
/// nothing. No preset flashes (WCAG 2.3.1: no more than three flashes a second).
/// </para>
/// </summary>
public static class BmPresets
{
    // The house curves: entrances decelerate, and what pops does so on a gentle spring.
    private static BmTransition Enter(double duration) => Bm.Tween(duration, BmEase.Out);
    private static BmTransition Springy(double bounce, double duration) => Bm.Spring(bounce: bounce, duration: duration);

    // ── Entrances and exits (use with BmotionAnimatePresence / PresenceSwitch / PresenceGroup for the exit) ─────

    /// <summary>Fades in, and out on exit.</summary>
    public static BmPreset Fade(double duration = 0.25) => new()
    {
        Initial = Bm.To(opacity: 0),
        Animate = Bm.To(opacity: 1),
        Exit = Bm.To(opacity: 0),
        Transition = Enter(duration),
    };

    /// <summary>
    /// Slides in from <paramref name="from"/> by <paramref name="distance"/> px (fading too, unless
    /// <paramref name="fade"/> is off), and back out that way on exit.
    /// </summary>
    public static BmPreset Slide(BmEdge from = BmEdge.Bottom, double distance = 24, bool fade = true, double duration = 0.35)
    {
        var (x, y) = Offset(from, distance);
        return new BmPreset
        {
            Initial = Bm.To(x: x, y: y, opacity: FadeTo(fade, 0)),
            Animate = Bm.To(x: 0, y: 0, opacity: FadeTo(fade, 1)),
            Exit = Bm.To(x: x, y: y, opacity: FadeTo(fade, 0)),
            Transition = Enter(duration),
        };
    }

    /// <summary>Grows in from <paramref name="from"/> of its size (fading too, unless <paramref name="fade"/> is off).</summary>
    public static BmPreset Scale(double from = 0.9, bool fade = true, double duration = 0.25) => new()
    {
        Initial = Bm.To(scale: from, opacity: FadeTo(fade, 0)),
        Animate = Bm.To(scale: 1, opacity: FadeTo(fade, 1)),
        Exit = Bm.To(scale: from, opacity: FadeTo(fade, 0)),
        Transition = Enter(duration),
    };

    /// <summary>Pops in on a spring from <paramref name="from"/> of its size - for badges, confirmations, new items.</summary>
    public static BmPreset Pop(double from = 0.5, double bounce = 0.45, double duration = 0.45) => new()
    {
        Initial = Bm.To(scale: from, opacity: 0),
        Animate = Bm.To(scale: 1, opacity: 1),
        Exit = Bm.To(scale: from, opacity: 0, transition: Bm.Tween(0.15, BmEase.In)),
        Transition = Springy(bounce, duration),
    };

    /// <summary>Comes into focus from a <paramref name="amount"/> px blur while fading in.</summary>
    public static BmPreset Blur(double amount = 10, double duration = 0.35) => new()
    {
        Initial = Bm.To(opacity: 0, filter: $"blur({BmotionCssFormat.Num(amount)}px)"),
        Animate = Bm.To(opacity: 1, filter: "blur(0px)"),
        Exit = Bm.To(opacity: 0, filter: $"blur({BmotionCssFormat.Num(amount)}px)"),
        Transition = Enter(duration),
    };

    /// <summary>Turns into place from <paramref name="degrees"/> while fading in.</summary>
    public static BmPreset Rotate(double degrees = -10, double duration = 0.35) => new()
    {
        Initial = Bm.To(rotate: degrees, opacity: 0),
        Animate = Bm.To(rotate: 0, opacity: 1),
        Exit = Bm.To(rotate: degrees, opacity: 0),
        Transition = Enter(duration),
    };

    /// <summary>Flips into view in 3D about <paramref name="axis"/> (a card turning over).</summary>
    public static BmPreset Flip(BmAxis axis = BmAxis.Y, double degrees = 90, double duration = 0.45)
        => new()
        {
            Initial = axis == BmAxis.Y ? Bm.To(perspective: 800, rotateY: degrees, opacity: 0) : Bm.To(perspective: 800, rotateX: degrees, opacity: 0),
            Animate = axis == BmAxis.Y ? Bm.To(perspective: 800, rotateY: 0, opacity: 1) : Bm.To(perspective: 800, rotateX: 0, opacity: 1),
            Exit = axis == BmAxis.Y ? Bm.To(perspective: 800, rotateY: -degrees, opacity: 0) : Bm.To(perspective: 800, rotateX: -degrees, opacity: 0),
            Transition = Enter(duration),
        };

    // ── Overlays ──────────────────────────────────────────────────────────────

    /// <summary>
    /// A dialog: rises slightly and scales up from 95% while fading in, and leaves faster than it came. Put
    /// <c>role="dialog"</c> and focus handling on the element itself.
    /// </summary>
    public static BmPreset Modal(double duration = 0.3) => new()
    {
        Initial = Bm.To(opacity: 0, scale: 0.95, y: 12),
        Animate = Bm.To(opacity: 1, scale: 1, y: 0),
        Exit = Bm.To(opacity: 0, scale: 0.97, y: 8, transition: Bm.Tween(duration * 0.6, BmEase.In)),
        Transition = Springy(0.12, duration),
    };

    /// <summary>
    /// A panel sliding in from <paramref name="edge"/> by its own full size (CSS <c>translate</c> percentages, so it
    /// works whatever the panel's size) - drawers, side sheets, bottom sheets.
    /// </summary>
    public static BmPreset Drawer(BmEdge edge = BmEdge.Right, double duration = 0.35)
    {
        string away = edge switch
        {
            BmEdge.Left => "-100% 0%",
            BmEdge.Top => "0% -100%",
            BmEdge.Bottom => "0% 100%",
            _ => "100% 0%",
        };
        return new BmPreset
        {
            Initial = Bm.To(css: new() { ["translate"] = away }),
            Animate = Bm.To(css: new() { ["translate"] = "0% 0%" }),
            Exit = Bm.To(css: new() { ["translate"] = away }),
            Transition = Bm.Tween(duration, BmEase.InOut),
        };
    }

    /// <summary>
    /// A dropdown or popover opening from the edge it hangs off (<paramref name="origin"/>: the side facing its
    /// trigger) - scaling from there while fading in.
    /// </summary>
    public static BmPreset Popover(BmEdge origin = BmEdge.Top, double duration = 0.18)
    {
        var (originX, originY) = origin switch
        {
            BmEdge.Bottom => (0.5, 1.0),
            BmEdge.Left => (0.0, 0.5),
            BmEdge.Right => (1.0, 0.5),
            _ => (0.5, 0.0),
        };
        var (x, y) = Offset(origin, 4);
        return new BmPreset
        {
            Initial = Bm.To(opacity: 0, scale: 0.96, x: x, y: y, originX: originX, originY: originY),
            Animate = Bm.To(opacity: 1, scale: 1, x: 0, y: 0, originX: originX, originY: originY),
            Exit = Bm.To(opacity: 0, scale: 0.98, originX: originX, originY: originY),
            Transition = Enter(duration),
        };
    }

    /// <summary>A tooltip: a quick, small fade and nudge - motion that doesn't get in the way of reading it.</summary>
    public static BmPreset Tooltip(BmEdge from = BmEdge.Bottom, double duration = 0.15)
    {
        var (x, y) = Offset(from, 4);
        return new BmPreset
        {
            Initial = Bm.To(opacity: 0, x: x, y: y, scale: 0.98),
            Animate = Bm.To(opacity: 1, x: 0, y: 0, scale: 1),
            Exit = Bm.To(opacity: 0),
            Transition = Enter(duration),
        };
    }

    /// <summary>A toast or notification arriving from <paramref name="from"/> on a spring, and leaving the same way.</summary>
    public static BmPreset Toast(BmEdge from = BmEdge.Bottom, double distance = 40) => new()
    {
        Initial = Bm.To(opacity: 0, scale: 0.95, x: Offset(from, distance).X, y: Offset(from, distance).Y),
        Animate = Bm.To(opacity: 1, scale: 1, x: 0, y: 0),
        Exit = Bm.To(opacity: 0, scale: 0.95, transition: Bm.Tween(0.2, BmEase.In)),
        Transition = Springy(0.25, 0.45),
    };

    // ── Hover and press feedback ──────────────────────────────────────────────

    /// <summary>Lifts on hover (and presses back down) - cards, tiles, list rows.</summary>
    public static BmPreset HoverLift(double lift = 4, double scale = 1.02) => new()
    {
        WhileHover = Bm.To(y: -lift, scale: scale),
        WhileTap = Bm.To(y: 0, scale: 1 - ((scale - 1) / 2)),
        Transition = Springy(0.2, 0.3),
    };

    /// <summary>Grows on hover.</summary>
    public static BmPreset HoverScale(double scale = 1.05) => new()
    {
        WhileHover = Bm.To(scale: scale),
        Transition = Springy(0.25, 0.3),
    };

    /// <summary>Shrinks a little while pressed (pointer or keyboard) - buttons and toggles.</summary>
    public static BmPreset Press(double scale = 0.96) => new()
    {
        WhileTap = Bm.To(scale: scale),
        Transition = Springy(0.3, 0.2),
    };

    // ── Attention (they play when the element mounts or the preset changes; key the element to replay one) ──────

    /// <summary>A gentle pulse - once, or as often as <paramref name="repeat"/> says (<see cref="BmRepeat.Forever"/>).</summary>
    public static BmPreset Pulse(double scale = 1.06, BmRepeat? repeat = null) => new()
    {
        Animate = Bm.To(scale: new double[] { 1, scale, 1 }),
        Transition = Bm.Tween(1.2, BmEase.InOut, repeat: repeat),
    };

    /// <summary>A horizontal shake - "that didn't work" (a rejected password, a failed drop).</summary>
    public static BmPreset Shake(double distance = 10) => new()
    {
        Animate = Bm.To(x: new[] { 0, -distance, distance, -distance * 0.8, distance * 0.8, -distance * 0.4, distance * 0.4, 0 }),
        Transition = Bm.Tween(0.5, BmEase.Linear),
    };

    /// <summary>A small rotation back and forth - "look here".</summary>
    public static BmPreset Wiggle(double degrees = 8) => new()
    {
        Animate = Bm.To(rotate: new[] { 0, -degrees, degrees, -degrees * 0.6, degrees * 0.6, 0 }),
        Transition = Bm.Tween(0.5, BmEase.InOut),
    };

    /// <summary>Two hops, the second smaller.</summary>
    public static BmPreset Jump(double height = 14) => new()
    {
        Animate = Bm.To(y: new[] { 0, -height, 0, -height * 0.4, 0 }),
        Transition = Bm.Tween(0.6, BmEase.Out),
    };

    /// <summary>A double beat - likes, favourites.</summary>
    public static BmPreset Heartbeat(double scale = 1.15) => new()
    {
        Animate = Bm.To(scale: new[] { 1, scale, 1, scale, 1 }),
        Transition = Bm.Tween(0.9, BmEase.InOut),
    };

    // ── Scroll ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reveals the element as it scrolls into view - fading and sliding in from <paramref name="from"/> once a
    /// <paramref name="amount"/> of it is visible, and staying revealed when <paramref name="once"/>.
    /// </summary>
    public static BmPreset Reveal(BmEdge from = BmEdge.Bottom, double distance = 32, bool once = true, double amount = 0.2)
    {
        var (x, y) = Offset(from, distance);
        return new BmPreset
        {
            Initial = Bm.To(opacity: 0, x: x, y: y),
            WhileInView = Bm.To(opacity: 1, x: 0, y: 0),
            Viewport = new BmViewport { Once = once, Amount = BmotionCssFormat.Num(amount) },
            Transition = Bm.Tween(0.6, BmEase.Out),
        };
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    /// <summary>Spins forever - for a loading indicator (put <c>role="status"</c> and a label on it).</summary>
    public static BmPreset Spin(double secondsPerTurn = 1) => new()
    {
        Animate = Bm.To(rotate: new double[] { 0, 360 }),
        Transition = Bm.Tween(secondsPerTurn, BmEase.Linear, repeat: BmRepeat.Forever),
    };

    /// <summary>
    /// The shimmer of a loading skeleton: sweeps the element's background across it forever. The element needs a
    /// gradient background twice its width, e.g.
    /// <c>background: linear-gradient(90deg, #eee 25%, #f8f8f8 50%, #eee 75%); background-size: 200% 100%;</c>.
    /// </summary>
    public static BmPreset Shimmer(double duration = 1.4) => new()
    {
        Animate = Bm.To(backgroundPosition: new[] { "100% 0%", "-100% 0%" }),
        Transition = Bm.Tween(duration, BmEase.Linear, repeat: BmRepeat.Forever),
    };

    // Opacity only when the preset fades: an absent opacity leaves the element's own alone.
    private static BmKeyframes? FadeTo(bool fade, double opacity) => fade ? (BmKeyframes)opacity : null;

    // An offset that sits on the given side: -y is up.
    private static (double X, double Y) Offset(BmEdge edge, double distance) => edge switch
    {
        BmEdge.Top => (0, -distance),
        BmEdge.Left => (-distance, 0),
        BmEdge.Right => (distance, 0),
        _ => (0, distance),
    };
}
