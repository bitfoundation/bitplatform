
namespace Bit.Bink;

/// <summary>How a drawing operation is composited with what is already on the surface.</summary>
public enum BinkBlendMode : byte
{
    /// <summary>Standard source-over alpha compositing.</summary>
    Normal = 0,
    /// <summary>Multiplies colors: what highlighters do on paper. Overlapping highlights get darker, black stays black.</summary>
    Multiply = 1,
    /// <summary>Screen (inverse multiply); lightens.</summary>
    Screen = 2,
    /// <summary>Erases: removes existing content where the shape is drawn (within its layer). Used by the pixel eraser.</summary>
    DestinationOut = 3,
}

/// <summary>Shape of the ends of a stroked path.</summary>
public enum BinkLineCap : byte
{
    /// <summary>Flat end exactly at the endpoint.</summary>
    Butt = 0,
    /// <summary>Semicircular end.</summary>
    Round = 1,
    /// <summary>Flat end extended by half the line width.</summary>
    Square = 2,
}

/// <summary>Shape of the corners of a stroked path.</summary>
public enum BinkLineJoin : byte
{
    /// <summary>Sharp corner.</summary>
    Miter = 0,
    /// <summary>Rounded corner.</summary>
    Round = 1,
    /// <summary>Cut-off corner.</summary>
    Bevel = 2,
}

/// <summary>Paint used to fill a path.</summary>
/// <param name="Color">Fill color (its alpha is multiplied with <paramref name="Opacity"/>).</param>
/// <param name="Opacity">Additional opacity 0..1 applied to the whole path as one unit, so self-overlaps do not darken.</param>
/// <param name="Blend">Blend mode.</param>
/// <param name="Rule">Fill rule for self-intersecting paths.</param>
public readonly record struct BinkFillPaint(BinkColor Color, float Opacity = 1f, BinkBlendMode Blend = BinkBlendMode.Normal, BinkFillRule Rule = BinkFillRule.NonZero)
{
    /// <summary>Effective alpha (color alpha times opacity).</summary>
    public float EffectiveAlpha => Color.Alpha * Math.Clamp(Opacity, 0f, 1f);
}

/// <summary>Paint used to stroke (outline) a path.</summary>
/// <param name="Color">Line color.</param>
/// <param name="Width">Line width in the current coordinate space.</param>
/// <param name="Opacity">Additional opacity 0..1 applied to the whole path as one unit.</param>
/// <param name="Blend">Blend mode.</param>
/// <param name="Cap">Line cap.</param>
/// <param name="Join">Line join.</param>
/// <param name="Dash">Optional dash pattern (on, off, ...) in the current coordinate space.</param>
public readonly record struct BinkStrokePaint(BinkColor Color, float Width, float Opacity = 1f, BinkBlendMode Blend = BinkBlendMode.Normal, BinkLineCap Cap = BinkLineCap.Round, BinkLineJoin Join = BinkLineJoin.Round, float[]? Dash = null)
{
    /// <summary>Effective alpha (color alpha times opacity).</summary>
    public float EffectiveAlpha => Color.Alpha * Math.Clamp(Opacity, 0f, 1f);
}

/// <summary>Horizontal text alignment relative to the text origin.</summary>
public enum BinkTextAlign : byte
{
    /// <summary>Text starts at the origin.</summary>
    Left = 0,
    /// <summary>Text is centered on the origin.</summary>
    Center = 1,
    /// <summary>Text ends at the origin.</summary>
    Right = 2,
}

/// <summary>Text style. Fonts are referenced by family name; the host resolves them (browser fonts for canvas, viewer fonts for SVG).</summary>
/// <param name="FontFamily">CSS font family list, e.g. <c>"Segoe UI, sans-serif"</c>.</param>
/// <param name="FontSize">Font size in document units.</param>
/// <param name="Color">Text color.</param>
/// <param name="Bold">Bold weight.</param>
/// <param name="Italic">Italic style.</param>
/// <param name="Align">Horizontal alignment.</param>
/// <param name="LineHeight">Line height as a multiple of the font size.</param>
public readonly record struct BinkTextStyle(string FontFamily, float FontSize, BinkColor Color, bool Bold = false, bool Italic = false, BinkTextAlign Align = BinkTextAlign.Left, float LineHeight = 1.25f)
{
    /// <summary>A neutral default text style.</summary>
    public static readonly BinkTextStyle Default = new("system-ui, -apple-system, Segoe UI, Roboto, sans-serif", 16f, BinkColor.Black);
}
