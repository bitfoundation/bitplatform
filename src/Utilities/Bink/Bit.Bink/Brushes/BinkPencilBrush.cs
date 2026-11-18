
namespace Bit.Bink;

/// <summary>
/// A pencil: thin, slightly translucent, with strong pressure response. Renders as a variable-width outline.
/// </summary>
public sealed record BinkPencilBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pencil</c>.</summary>
    public const string KindName = "pencil";

    /// <summary>Creates a pencil with defaults (2 units wide, 85% opacity).</summary>
    public BinkPencilBrush()
    {
        Width = 2f;
        Opacity = 0.85f;
        PressureCurve = new BinkPressureCurve(BinkPressureCurveKind.EaseIn, 0.35f, 1f);
        Smoothing = 0.35f;
        Spacing = 0.15f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}
