namespace Bit.BlazorUI.Build;

/// <summary>An icon font's stylesheet, one rule per glyph between a part before and a part after them.</summary>
public sealed class IconSet(string bundle, string file, string classPrefix, string? font = null)
{
    /// <summary>The stylesheet's path under the package's wwwroot ("styles/bit.blazorui.icons.css").</summary>
    public string Bundle { get; } = bundle;

    /// <summary>The glyphs and the parts around them, relative to the package's assets folder.</summary>
    public string File { get; } = file;

    /// <summary>What a glyph's class starts with ("bit-icon--"), the rest being the icon's name.</summary>
    public string ClassPrefix { get; } = classPrefix;

    /// <summary>
    /// The WOFF2 font the stylesheet's @font-face loads, under the package's wwwroot ("fonts/FabMDL2.4.66.woff2"); null for
    /// a stylesheet that loads no single one, whose font is never cut down.
    /// </summary>
    public string? Font { get; } = font;
}
