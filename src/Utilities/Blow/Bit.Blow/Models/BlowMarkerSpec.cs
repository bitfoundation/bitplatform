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
        $"{(Type == BlowMarkerType.ArrowClosed ? "c" : "o")}-{Sanitize(Color)}{(Color is null && FallbackColor is not null ? "-f" + Sanitize(FallbackColor) : "")}-{Width:0.##}-{Height:0.##}");

    /// <summary>
    /// The stroke/fill used by the marker. <c>context-stroke</c> makes the arrowhead inherit the
    /// edge's own colour, which is what an edge without an explicit marker colour wants.
    /// </summary>
    public string Paint => Color ?? "context-stroke";

    /// <summary>
    /// The colour an arrowhead without a <see cref="Color"/> is painted where the browser has no <c>context-stroke</c>
    /// (WebKit: Safari, and every browser on iOS), which would otherwise paint it not at all: the edge's own
    /// <see cref="BlowEdge.Stroke"/>, or null for the theme's edge colour. Part of <see cref="Key"/>, so edges of
    /// different colours do not share a head there.
    /// </summary>
    internal string? FallbackColor { get; init; }

    /// <summary>
    /// The inline style that carries <see cref="FallbackColor"/>, or null when the head has a colour of its own. A
    /// browser that does not know <c>context-stroke</c> drops that declaration and keeps the colour before it; one
    /// that does takes the edge's live colour as before, the selected colour included.
    /// </summary>
    internal string? FallbackStyle
    {
        get
        {
            if (Color is not null) return null;
            var color = FallbackColor ?? "var(--bit-blow-edge-stroke)";
            return Type == BlowMarkerType.ArrowClosed
                ? $"fill:{color};stroke:{color};fill:context-stroke;stroke:context-stroke"
                : $"stroke:{color};stroke:context-stroke";
        }
    }

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
