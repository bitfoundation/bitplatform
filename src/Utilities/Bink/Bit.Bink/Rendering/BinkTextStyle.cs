
namespace Bit.Bink;

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
