
namespace Bit.Bink;

/// <summary>
/// A pressure-sensitive pen: variable width outline with round caps and a subtle taper at the end.
/// The default brush for handwriting and signatures.
/// </summary>
public sealed record BinkPenBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pen</c>.</summary>
    public const string KindName = "pen";

    /// <summary>Creates a pen with sensible defaults: black, 4 units wide, the S-curve pressure response.</summary>
    public BinkPenBrush()
    {
        Width = 4f;
        PressureCurve = BinkPressureCurve.Pen;
        TaperEnd = 6f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}
