namespace Bit.Bmotion;

/// <summary>
/// Bends the straight line an element would travel between two points into a curve - motion.dev's
/// <c>arc()</c>. Attach it to a transition's <see cref="BmotionTransition.Path"/> and animate
/// <c>x</c> and <c>y</c> together:
/// <code>
/// &lt;Bmotion Animate="Bm.To(x: 220, y: 90)"
///          Transition="Bm.Tween(0.8, path: Bm.Arc(strength: 0.8, rotate: 1))"&gt;
///     &lt;div class="card" /&gt;
/// &lt;/Bmotion&gt;
/// </code>
/// <para>
/// There is no path data to author: the curve is generated from wherever the element is to wherever
/// it is going, so it keeps working when either end moves. The timing still comes from the
/// transition - a spring on a path arcs <em>and</em> overshoots.
/// </para>
/// <para>
/// It needs both <c>x</c> and <c>y</c> as single values (not keyframe sequences) in the same
/// target; anything else animates along the ordinary straight line.
/// </para>
/// </summary>
public sealed class BmotionArc
{
    /// <summary>
    /// How far the curve bends, as a fraction of the distance travelled: <c>0</c> is a straight
    /// line and <c>1</c> peaks a full travel-distance away from it. Default <c>0.5</c>.
    /// Clamped to <c>[0, 1]</c>.
    /// </summary>
    public double Strength { get; set; } = 0.5;

    /// <summary>
    /// Where along the journey the curve crests: <c>0</c> pulls the apex towards the start,
    /// <c>1</c> towards the end. Default <c>0.5</c> (a symmetric arc). Clamped to <c>[0, 1]</c>.
    /// <para>
    /// It skews where the high point sits in space; the deepest departure from the straight line
    /// still happens halfway through the animation's own timing.
    /// </para>
    /// </summary>
    public double Peak { get; set; } = 0.5;

    /// <summary>Which side the arc bulges towards. Default <see cref="BmotionArcDirection.Auto"/>.</summary>
    public BmotionArcDirection Direction { get; set; } = BmotionArcDirection.Auto;

    /// <summary>
    /// How much the element turns to follow the curve: <c>0</c> (default) keeps it upright,
    /// <c>1</c> points it exactly along the tangent, values between scale the effect. Clamped to
    /// <c>[0, 1]</c>.
    /// <para>
    /// Non-zero values drive the element's <c>rotate</c>, so don't also animate <c>rotate</c> in
    /// the same target - the arc owns it.
    /// </para>
    /// </summary>
    public double Rotate { get; set; }

    /// <summary>Whether this arc turns the element to follow the curve.</summary>
    internal bool FollowsTangent => Sanitize(Rotate) > 0;

    /// <summary>
    /// Structural comparison so an arc recreated inline on every render doesn't read as a
    /// transition change.
    /// </summary>
    internal static bool AreEquivalent(BmotionArc? a, BmotionArc? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Strength == b.Strength && a.Peak == b.Peak
            && a.Direction == b.Direction && a.Rotate == b.Rotate;
    }

    private static double Sanitize(double v) => !double.IsFinite(v) ? 0 : v < 0 ? 0 : v > 1 ? 1 : v;

    /// <summary>
    /// Builds the quadratic Bézier that carries the element from <paramref name="fromX"/>,
    /// <paramref name="fromY"/> to <paramref name="toX"/>, <paramref name="toY"/>.
    /// </summary>
    internal BmotionArcCurve BuildCurve(double fromX, double fromY, double toX, double toY)
    {
        double strength = Sanitize(Strength), peak = Sanitize(Peak), rotate = Sanitize(Rotate);
        double dx = toX - fromX, dy = toY - fromY;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        // No distance (or no bend) means no curve to build: a straight-line Bézier whose control
        // point sits on the chord, which interpolates exactly like the ordinary path.
        if (distance < 1e-9 || strength <= 0)
            return new BmotionArcCurve(fromX, fromY, fromX + dx * peak, fromY + dy * peak, toX, toY, rotate);

        // Unit normal to the chord (a 90° turn of the direction vector, screen coordinates: y down).
        double nx = -dy / distance, ny = dx / distance;
        bool flip = Direction switch
        {
            BmotionArcDirection.Clockwise => ny > 0,
            BmotionArcDirection.CounterClockwise => ny < 0,
            // Auto: bulge upward (negative y on screen). A purely vertical move has ny == 0, where
            // both normals are equally "up" - keep the unflipped one so repeated calls agree.
            _ => ny > 0,
        };
        if (flip) { nx = -nx; ny = -ny; }

        // A quadratic Bézier reaches only half way to its control point, so offsetting by twice the
        // requested height makes `strength: 1` peak a full travel-distance off the chord, as documented.
        double offset = 2 * strength * distance;
        double controlX = fromX + dx * peak + nx * offset;
        double controlY = fromY + dy * peak + ny * offset;
        return new BmotionArcCurve(fromX, fromY, controlX, controlY, toX, toY, rotate);
    }
}
