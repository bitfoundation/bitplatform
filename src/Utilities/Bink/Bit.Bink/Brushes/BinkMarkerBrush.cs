
namespace Bit.Bink;

/// <summary>
/// A marker: constant width, round caps, opaque. Renders as a stroked smooth centerline, which keeps geometry small.
/// </summary>
public sealed record BinkMarkerBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>marker</c>.</summary>
    public const string KindName = "marker";

    /// <summary>Creates a marker with defaults (8 units wide, no pressure).</summary>
    public BinkMarkerBrush()
    {
        Width = 8f;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkCenterlineBrushes.Build(points, builder, Width, BinkLineCap.Round, BinkLineJoin.Round);

    /// <inheritdoc />
    public override BinkRect GetBounds(BinkRect pointBounds) => base.GetBounds(BinkCenterlineBrushes.WithSplineOvershoot(pointBounds));
}
