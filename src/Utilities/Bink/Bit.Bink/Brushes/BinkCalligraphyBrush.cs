using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// A broad-nib pen: a flat nib held at a fixed angle, so the line is thick where the stroke moves across the nib and
/// thin where it moves along it, as a calligraphy pen or a chisel-tip marker draws. Pressure, when the device has it,
/// widens the nib.
/// </summary>
/// <remarks>
/// The ink is the area the nib sweeps: for each segment, the hull of the nib at both ends. The hulls are filled
/// together with the non-zero rule, so they join without seams and overlapping parts do not darken. Tapering does not
/// apply: a broad nib's ends are its own shape.
/// </remarks>
public sealed record BinkCalligraphyBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>calligraphy</c>.</summary>
    public const string KindName = "calligraphy";

    /// <summary>Creates a broad nib 10 units wide at 45 degrees.</summary>
    public BinkCalligraphyBrush()
    {
        Width = 10f;
        PressureCurve = new BinkPressureCurve(BinkPressureCurveKind.EaseOut, 0.6f, 1f);
        SimulatePressure = false;
        Smoothing = 0.4f;
        Spacing = 0.1f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <summary>
    /// The angle of the nib in degrees, clockwise from horizontal: at 45, strokes from top left to bottom right are
    /// thin and strokes from top right to bottom left are thick, as an italic hand is written.
    /// </summary>
    public float NibAngle { get; init; } = 45f;

    /// <summary>
    /// How thick the nib is, as a fraction of its <see cref="BinkBrush.Width"/>: the width of a stroke drawn along the
    /// nib. Real nibs are never perfectly thin.
    /// </summary>
    public float NibThickness { get; init; } = 0.15f;

    /// <summary>
    /// Whether the nib turns with the pen, as a real one turns in the hand. With a pen that reports barrel rotation
    /// (twist), the nib turns by it. Otherwise, with one that reports tilt, it turns by how far the direction the pen
    /// leans is from <see cref="PenLean"/>: leaning that way, the nib is at <see cref="NibAngle"/>. As the pen is
    /// raised upright its lean loses its direction, and the nib eases back to <see cref="NibAngle"/>, which it reaches
    /// when the pen is straight up. Without tilt or twist, a mouse or a finger, the nib stays at <see cref="NibAngle"/>.
    /// Off by default.
    /// </summary>
    /// <remarks>
    /// Each point's angle depends on that point alone, so a stroke cut in two by an eraser keeps the same ink in both
    /// parts.
    /// </remarks>
    public bool FollowPen { get; init; }

    /// <summary>
    /// The direction, in degrees clockwise from the right, that the pen leans when the nib is at <see cref="NibAngle"/>,
    /// for <see cref="FollowPen"/>. 45, the default, is a right hand's: the pen leaning toward the lower right of the
    /// page. 135 suits a left hand.
    /// </summary>
    public float PenLean { get; init; } = 45f;

    /// <summary>
    /// Tilt in degrees below which the direction the pen leans is too uncertain to turn the nib all the way: below it the
    /// turn shrinks with the tilt, to none when the pen is upright.
    /// </summary>
    private const float MinFollowTilt = 10f;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, float> GetParameters()
        => new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["nibAngle"] = NibAngle,
            ["nibThickness"] = NibThickness,
            // Written whether on or off, so a reader whose own default differs still reads what was drawn; a build
            // that predates them ignores both.
            ["followPen"] = FollowPen ? 1f : 0f,
            ["penLean"] = PenLean,
        };

    /// <inheritdoc />
    public override BinkBrush WithParameters(IReadOnlyDictionary<string, float> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return this with
        {
            NibAngle = parameters.TryGetValue("nibAngle", out float angle) && float.IsFinite(angle) ? angle % 360f : NibAngle,
            NibThickness = parameters.TryGetValue("nibThickness", out float thickness) && float.IsFinite(thickness) ? Math.Clamp(thickness, 0.01f, 1f) : NibThickness,
            FollowPen = parameters.TryGetValue("followPen", out float follow) ? follow >= 0.5f : FollowPen,
            PenLean = parameters.TryGetValue("penLean", out float lean) && float.IsFinite(lean) ? lean % 360f : PenLean,
        };
    }

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        if (!float.IsFinite(NibAngle)) throw new InvalidOperationException("The nib angle must be a finite number of degrees.");
        if (!(NibThickness > 0f) || NibThickness > 1f) throw new InvalidOperationException($"The nib thickness must be between 0 and 1 (was {NibThickness}).");
    }

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Reset();
        int n = points.Count;
        if (n == 0) return BinkStrokeGeometry.Empty;

        float radians = NibAngle * MathF.PI / 180f;
        float thickness = Math.Clamp(float.IsFinite(NibThickness) ? NibThickness : 0.15f, 0.01f, 1f);
        BinkPressureCurve curve = PressureCurve ?? BinkPressureCurve.Linear;
        float lean = (float.IsFinite(PenLean) ? PenLean : 45f) * MathF.PI / 180f;
        var follow = new PenFollower(FollowPen, points, radians, lean);

        Span<Vector2> corners = stackalloc Vector2[8];
        Span<Vector2> sorted = stackalloc Vector2[8];
        // The monotone chain can hold up to twice the points while it builds the hull.
        Span<Vector2> hull = stackalloc Vector2[17];
        Nib(points, 0, follow.AngleAt(points, 0), thickness, curve, corners[..4]);
        if (n == 1)
        {
            builder.AddPolygon(corners[..4]);
            return Finish(builder);
        }
        for (int i = 1; i < n; i++)
        {
            // The nib at the start of the segment is the one at the end of the last; only the far end is new.
            if (i > 1) corners[4..].CopyTo(corners);
            Nib(points, i, follow.AngleAt(points, i), thickness, curve, corners[4..]);
            // Sorted in a copy: the corners at this end are the next segment's start.
            corners.CopyTo(sorted);
            int count = ConvexHull(sorted, hull);
            if (count >= 3) builder.AddPolygon(hull[..count]);
        }
        return Finish(builder);
    }

    private BinkStrokeGeometry Finish(BinkPathBuilder builder)
        => builder.IsEmpty ? BinkStrokeGeometry.Empty : new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, BinkLineCap.Butt, BinkLineJoin.Miter, builder.Bounds.Inflate(1f));

    /// <summary>
    /// The nib's angle point by point: <see cref="NibAngle"/>, turned by the pen's twist or by how far its lean is from
    /// <see cref="PenLean"/>, when <see cref="FollowPen"/> is on and the device reports them. Each angle is read from its
    /// own point: one carried over from earlier points would make the part of a stroke an eraser leaves start afresh at
    /// <see cref="NibAngle"/> and draw differently from the same points in the whole stroke.
    /// </summary>
    private readonly struct PenFollower
    {
        private readonly bool _twist;
        private readonly bool _tilt;
        private readonly float _base;
        private readonly float _lean;

        public PenFollower(bool enabled, BinkStrokePointsView points, float baseRadians, float leanRadians)
        {
            _base = baseRadians;
            _lean = leanRadians;
            _twist = enabled && HasAny(points.Twist);
            _tilt = enabled && !_twist && (HasAny(points.TiltX) || HasAny(points.TiltY));
        }

        public float AngleAt(BinkStrokePointsView points, int index)
        {
            if (_twist)
            {
                float twist = points.Twist[index];
                return float.IsFinite(twist) ? _base + twist * MathF.PI / 180f : _base;
            }
            if (!_tilt) return _base;
            float tx = points.TiltX[index], ty = points.TiltY[index];
            if (!float.IsFinite(tx) || !float.IsFinite(ty)) return _base;
            float tilt = MathF.Max(MathF.Abs(tx), MathF.Abs(ty));
            if (tilt == 0f) return _base;
            // The direction the pen leans, from its tilt in the two planes.
            float azimuth = MathF.Atan2(MathF.Tan(Math.Clamp(ty, -89f, 89f) * MathF.PI / 180f), MathF.Tan(Math.Clamp(tx, -89f, 89f) * MathF.PI / 180f));
            float turn = azimuth - _lean;
            if (tilt >= MinFollowTilt) return _base + turn;
            // Nearly upright the direction is uncertain, so it counts for less the more upright the pen is. A nib turned
            // half a turn is the same nib, so the turn is taken the short way round, within a quarter turn either side,
            // before it is scaled.
            turn -= MathF.PI * MathF.Round(turn / MathF.PI);
            return _base + turn * (tilt / MinFollowTilt);
        }

        private static bool HasAny(ReadOnlySpan<float> values)
        {
            foreach (float v in values) if (v != 0f) return true;
            return false;
        }
    }

    /// <summary>The four corners of the nib at a point, held at <paramref name="radians"/>, its length scaled by the pressure there.</summary>
    private void Nib(BinkStrokePointsView points, int index, float radians, float thickness, BinkPressureCurve curve, Span<Vector2> corners)
    {
        var along = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        Vector2 across = BinkGeometryMath.Perpendicular(along);
        float pressure = index < points.Pressure.Length ? points.Pressure[index] : 0.5f;
        float half = Width / 2f * MathF.Max(curve.Apply(Math.Clamp(float.IsFinite(pressure) ? pressure : 0.5f, 0f, 1f)), 0.05f);
        Vector2 p = points.PositionAt(index);
        Vector2 a = along * half;
        Vector2 c = across * (half * thickness);
        // Corners in order around the nib, so a lone nib is already a polygon.
        corners[0] = p - a - c;
        corners[1] = p + a - c;
        corners[2] = p + a + c;
        corners[3] = p - a + c;
    }

    /// <summary>
    /// The convex hull of the points, by Andrew's monotone chain, always wound the same way: the hulls of every
    /// segment are filled together under the non-zero rule, which only unions shapes that turn the same direction.
    /// </summary>
    private static int ConvexHull(Span<Vector2> points, Span<Vector2> hull)
    {
        points.Sort(static (a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        int k = 0;
        for (int i = 0; i < points.Length; i++)
        {
            while (k >= 2 && BinkGeometryMath.Cross(hull[k - 1] - hull[k - 2], points[i] - hull[k - 2]) <= 0) k--;
            hull[k++] = points[i];
        }
        for (int i = points.Length - 2, lower = k + 1; i >= 0; i--)
        {
            while (k >= lower && BinkGeometryMath.Cross(hull[k - 1] - hull[k - 2], points[i] - hull[k - 2]) <= 0) k--;
            hull[k++] = points[i];
        }
        // The last point is the first again.
        return Math.Max(k - 1, 0);
    }
}
