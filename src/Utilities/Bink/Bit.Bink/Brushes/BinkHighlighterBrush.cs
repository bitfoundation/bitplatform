
namespace Bit.Bink;

/// <summary>
/// A highlighter: wide, translucent, flat caps, multiply blend so text underneath stays legible and
/// overlapping highlights do not turn opaque.
/// </summary>
public sealed record BinkHighlighterBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>highlighter</c>.</summary>
    public const string KindName = "highlighter";

    /// <summary>Creates a yellow highlighter (20 units wide, 45% opacity, multiply).</summary>
    public BinkHighlighterBrush()
    {
        Color = new BinkColor(255, 235, 59);
        Width = 20f;
        Opacity = 0.45f;
        BlendMode = BinkBlendMode.Multiply;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
        Smoothing = 0.6f;
        Spacing = 0.2f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkCenterlineBrushes.Build(points, builder, Width, BinkLineCap.Butt, BinkLineJoin.Round);

    /// <inheritdoc />
    public override BinkRect GetBounds(BinkRect pointBounds) => base.GetBounds(BinkCenterlineBrushes.WithSplineOvershoot(pointBounds));
}
