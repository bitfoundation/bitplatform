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

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, float> GetParameters()
        => new Dictionary<string, float>(StringComparer.Ordinal) { ["nibAngle"] = NibAngle, ["nibThickness"] = NibThickness };

    /// <inheritdoc />
    public override BinkBrush WithParameters(IReadOnlyDictionary<string, float> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return this with
        {
            NibAngle = parameters.TryGetValue("nibAngle", out float angle) && float.IsFinite(angle) ? angle % 360f : NibAngle,
            NibThickness = parameters.TryGetValue("nibThickness", out float thickness) && float.IsFinite(thickness) ? Math.Clamp(thickness, 0.01f, 1f) : NibThickness,
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
        var along = new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        Vector2 across = BinkGeometryMath.Perpendicular(along);
        float thickness = Math.Clamp(float.IsFinite(NibThickness) ? NibThickness : 0.15f, 0.01f, 1f);
        BinkPressureCurve curve = PressureCurve ?? BinkPressureCurve.Linear;

        Span<Vector2> corners = stackalloc Vector2[8];
        Span<Vector2> sorted = stackalloc Vector2[8];
        // The monotone chain can hold up to twice the points while it builds the hull.
        Span<Vector2> hull = stackalloc Vector2[17];
        Nib(points, 0, along, across, thickness, curve, corners[..4]);
        if (n == 1)
        {
            builder.AddPolygon(corners[..4]);
            return Finish(builder);
        }
        for (int i = 1; i < n; i++)
        {
            // The nib at the start of the segment is the one at the end of the last; only the far end is new.
            if (i > 1) corners[4..].CopyTo(corners);
            Nib(points, i, along, across, thickness, curve, corners[4..]);
            // Sorted in a copy: the corners at this end are the next segment's start.
            corners.CopyTo(sorted);
            int count = ConvexHull(sorted, hull);
            if (count >= 3) builder.AddPolygon(hull[..count]);
        }
        return Finish(builder);
    }

    private BinkStrokeGeometry Finish(BinkPathBuilder builder)
        => builder.IsEmpty ? BinkStrokeGeometry.Empty : new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, BinkLineCap.Butt, BinkLineJoin.Miter, builder.Bounds.Inflate(1f));

    /// <summary>The four corners of the nib at a point, its length scaled by the pressure there.</summary>
    private void Nib(BinkStrokePointsView points, int index, Vector2 along, Vector2 across, float thickness, BinkPressureCurve curve, Span<Vector2> corners)
    {
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
