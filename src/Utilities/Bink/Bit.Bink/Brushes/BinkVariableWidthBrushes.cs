
namespace Bit.Bink;

/// <summary>Shared geometry helpers for brushes that render variable-width outlines.</summary>
public static class BinkVariableWidthBrushes
{
    /// <summary>Builds outline geometry for a brush.</summary>
    public static BinkStrokeGeometry Build(BinkStrokePointsView points, BinkPathBuilder builder, BinkBrush brush, bool roundCaps)
    {
        var options = new BinkOutlineOptions(brush.Width)
        {
            PressureCurve = brush.PressureCurve,
            TaperStart = brush.TaperStart,
            TaperEnd = brush.TaperEnd,
            RoundCaps = roundCaps,
            Smooth = true,
        };
        BinkRect bounds = BinkStrokeOutliner.Build(points, options, builder);
        if (builder.IsEmpty) return BinkStrokeGeometry.Empty;
        return new BinkStrokeGeometry(builder.ToGeometry(), BinkStrokeGeometryMode.Fill, 0f, BinkLineCap.Round, BinkLineJoin.Round, bounds.Inflate(1f));
    }
}
