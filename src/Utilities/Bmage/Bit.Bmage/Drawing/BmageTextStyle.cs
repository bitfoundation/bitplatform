namespace Bit.Bmage;

/// <summary>Text appearance: font, size, colours and alignment.</summary>
public sealed record BmageTextStyle
{
    /// <summary>Font family name. Resolution depends on the rasteriser in use.</summary>
    public string FontFamily { get; init; } = "sans-serif";

    /// <summary>Font size in pixels (em size).</summary>
    public float FontSize { get; init; } = 16f;

    public bool Bold { get; init; }
    public bool Italic { get; init; }

    /// <summary>Fill colour, or null to skip filling.</summary>
    public BmageRgba32? FillColor { get; init; } = BmageRgba32.Black;

    /// <summary>Outline colour, or null to skip outlining.</summary>
    public BmageRgba32? StrokeColor { get; init; }

    /// <summary>Outline style used when <see cref="StrokeColor"/> is set.</summary>
    public BmageStrokeStyle Stroke { get; init; } = BmageStrokeStyle.Default;

    /// <summary>Horizontal alignment of each line relative to the drawing position.</summary>
    public BmageTextAlignment Alignment { get; init; } = BmageTextAlignment.Left;

    /// <summary>Vertical meaning of the drawing position.</summary>
    public BmageTextBaseline Baseline { get; init; } = BmageTextBaseline.Alphabetic;

    /// <summary>Line spacing as a multiple of the font size.</summary>
    public float LineHeight { get; init; } = 1.2f;

    /// <summary>Extra spacing added between characters, in pixels.</summary>
    public float LetterSpacing { get; init; }

    /// <summary>Maximum line width in pixels before wrapping, or null for no wrapping.</summary>
    public float? MaxWidth { get; init; }

    /// <summary>Returns a style scaled for a proxy image.</summary>
    public BmageTextStyle ForScale(double scale) => scale == 1 ? this : this with
    {
        FontSize = (float)(FontSize * scale),
        LetterSpacing = (float)(LetterSpacing * scale),
        MaxWidth = MaxWidth is { } w ? (float)(w * scale) : null,
        Stroke = Stroke.ForScale(scale),
    };
}
