using System.Globalization;

namespace Bit.Blow;

/// <summary>
/// One concrete arrowhead: a shape plus the colour and size it is drawn at. Edges that ask for
/// the same combination share a single SVG <c>&lt;marker&gt;</c> definition, so a graph with a
/// thousand identically-styled edges still only declares one arrowhead.
/// </summary>
public readonly record struct BlowMarkerSpec(BlowMarkerType Type, string? Color, double Width, double Height)
{
    /// <summary>Marker size used when an edge does not ask for a specific one.</summary>
    public const double DefaultSize = 7;

    /// <summary>
    /// A stable, DOM-id-safe key for this spec. Two specs that render identically produce the
    /// same key, which is what lets the definitions be deduplicated.
    /// </summary>
    public string Key => string.Create(CultureInfo.InvariantCulture,
        $"{(Type == BlowMarkerType.ArrowClosed ? "c" : "o")}-{Sanitize(Color)}-{Width:0.##}-{Height:0.##}");

    /// <summary>
    /// The stroke/fill used by the marker. <c>context-stroke</c> makes the arrowhead inherit the
    /// edge's own colour, which is what an edge without an explicit marker colour wants.
    /// </summary>
    public string Paint => Color ?? "context-stroke";

    /// <summary>
    /// Reduces a CSS colour to characters that are legal (and unambiguous) inside an element id.
    /// Colours like <c>rgb(1, 2, 3)</c> and <c>#f00</c> both have to survive this intact.
    /// </summary>
    private static string Sanitize(string? color)
    {
        if (string.IsNullOrEmpty(color)) return "d";

        var sb = new System.Text.StringBuilder(color.Length);
        foreach (var ch in color)
        {
            if (char.IsAsciiLetterOrDigit(ch)) sb.Append(ch);
            else sb.Append('_').Append(((int)ch).ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }
}
