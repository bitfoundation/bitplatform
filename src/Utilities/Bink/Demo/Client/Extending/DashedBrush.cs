using System.Numerics;

namespace Bit.Bink.Demo.Client.Extending;

/// <summary>
/// A brush of the demo's own: a constant-width line broken into dashes.
/// </summary>
/// <remarks>
/// It is the whole contract of a custom brush in one place: a <see cref="Kind"/> that names it in a saved document,
/// geometry built from the stroke's points, and a setting of its own (<see cref="DashLength"/>) that is written with
/// every stroke and read back through <see cref="WithParameters"/>. A reader that does not know the kind draws the
/// stroke as a plain pen and keeps the kind and the setting, so passing a document through it loses nothing.
/// </remarks>
public sealed record DashedBrush : BinkBrush
{
    /// <summary>The kind written into saved documents.</summary>
    public const string KindName = "demo-dashed";

    /// <summary>The shortest and longest dash a saved document may ask for.</summary>
    public const float MinDash = 2f, MaxDash = 80f;

    /// <summary>Creates a 3 unit wide dashed line.</summary>
    public DashedBrush()
    {
        Width = 3f;
        // A dashed line keeps one width whatever the pressure.
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <summary>The length of each dash, in document units. The gaps are two thirds of it.</summary>
    public float DashLength { get; init; } = 12f;

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, float> GetParameters()
        => new Dictionary<string, float>(StringComparer.Ordinal) { ["dashLength"] = DashLength };

    /// <inheritdoc />
    /// <remarks>A file or a collaborator can say anything, so what is read back is clamped.</remarks>
    public override BinkBrush WithParameters(IReadOnlyDictionary<string, float> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        return this with
        {
            DashLength = parameters.TryGetValue("dashLength", out float length) && float.IsFinite(length)
                ? Math.Clamp(length, MinDash, MaxDash)
                : DashLength,
        };
    }

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Reset();
        if (points.Count == 1)
        {
            // A tap leaves a dot: a dash of no length, made round by its caps.
            Vector2 dot = points.PositionAt(0);
            builder.MoveTo(dot);
            builder.LineTo(dot + new Vector2(0.01f, 0f));
        }
        else if (points.Count > 1)
        {
            float dash = Math.Clamp(DashLength, MinDash, MaxDash);
            float gap = dash * 2f / 3f;
            bool drawing = true;
            float left = dash;
            Vector2 a = points.PositionAt(0);
            builder.MoveTo(a);
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 b = points.PositionAt(i);
                float length = Vector2.Distance(a, b);
                // Walk along the segment, ending a dash or a gap wherever the pattern says, so the dashes keep their
                // length however far apart the points are.
                while (length > 0f && length >= left)
                {
                    a += (b - a) * (left / length);
                    length -= left;
                    if (drawing) builder.LineTo(a);
                    else builder.MoveTo(a);
                    drawing = !drawing;
                    left = drawing ? dash : gap;
                }
                left -= length;
                if (drawing) builder.LineTo(b);
                a = b;
            }
        }

        // A centerline is stroked at the brush's width, which is far less geometry than an outline.
        return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Centerline, Width, BinkLineCap.Round, BinkLineJoin.Round, points.Bounds.Inflate(Width));
    }
}
