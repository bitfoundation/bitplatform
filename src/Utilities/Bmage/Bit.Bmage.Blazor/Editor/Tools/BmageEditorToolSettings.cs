namespace Bit.Bmage;

/// <summary>The current drawing style shared by the annotation tools.</summary>
public sealed record BmageEditorToolSettings
{
    public BmageRgba32 StrokeColor { get; init; } = new(230, 40, 40);
    public BmageRgba32 FillColor { get; init; } = new(230, 40, 40, 60);
    public bool Filled { get; init; }
    public float StrokeWidth { get; init; } = 4f;
    public bool Dashed { get; init; }
    public float Opacity { get; init; } = 1f;
    public float FontSize { get; init; } = 24f;
    public string FontFamily { get; init; } = "system-ui, sans-serif";
    public bool Bold { get; init; }
    public BmageRedactionMode RedactionMode { get; init; } = BmageRedactionMode.Solid;
    public float RedactionStrength { get; init; } = 14f;
    public BmageRgba32 HighlightColor { get; init; } = new(255, 235, 59, 110);

    /// <summary>The brush and eraser diameter in image pixels.</summary>
    public float BrushSize { get; init; } = 24f;

    /// <summary>How crisp the brush's edge is, in [0,1].</summary>
    public float BrushHardness { get; init; } = 0.8f;

    /// <summary>How far a colour may differ and still be filled or erased by the fill and magic eraser tools, in [0,1].</summary>
    public float Tolerance { get; init; } = 0.12f;

    /// <summary>Builds the shape style used by the outline tools.</summary>
    public BmageShapeStyle ToShapeStyle() => new()
    {
        StrokeColor = StrokeColor,
        FillColor = Filled ? FillColor : null,
        Stroke = new BmageStrokeStyle
        {
            Width = StrokeWidth,
            Cap = BmageLineCap.Round,
            Join = BmageLineJoin.Round,
            DashPattern = Dashed ? [StrokeWidth * 3, StrokeWidth * 2] : null,
        },
    };

    /// <summary>Builds the text style used by the text tools.</summary>
    public BmageTextStyle ToTextStyle() => new()
    {
        FontFamily = FontFamily,
        FontSize = FontSize,
        Bold = Bold,
        FillColor = StrokeColor,
    };
}
