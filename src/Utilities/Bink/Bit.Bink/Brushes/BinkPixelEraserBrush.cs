
namespace Bit.Bink;

/// <summary>
/// A pixel eraser. Strokes drawn with it are stored as vector elements that erase whatever is beneath them
/// within the same layer (destination-out compositing). Unlike the stroke and segment erasers this does not
/// modify other elements, so it can be undone and re-rendered exactly, and exports as an SVG mask.
/// </summary>
public sealed record BinkPixelEraserBrush : BinkBrush
{
    /// <summary>The serialized kind: <c>pixel-eraser</c>.</summary>
    public const string KindName = "pixel-eraser";

    /// <summary>Creates a 20-unit eraser.</summary>
    public BinkPixelEraserBrush()
    {
        Width = 20f;
        BlendMode = BinkBlendMode.DestinationOut;
        PressureCurve = BinkPressureCurve.Constant;
        SimulatePressure = false;
        Smoothing = 0.3f;
    }

    /// <inheritdoc />
    public override string Kind => KindName;

    /// <inheritdoc />
    public override BinkStrokeGeometry BuildGeometry(BinkStrokePointsView points, BinkPathBuilder builder)
        => BinkVariableWidthBrushes.Build(points, builder, this, roundCaps: true);
}
