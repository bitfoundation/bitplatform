using System.Globalization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Borm;

/// <summary>
/// The value of a <see cref="BormFieldType.Signature"/> field: an SVG image as a <c>data:</c> URL, which an
/// <c>&lt;img&gt;</c> shows as it is, a document embeds as it is, and a server stores as the text it is.
/// </summary>
/// <remarks>
/// A drawn signature is the strokes as one path, cropped to what was drawn; a typed one is the name, in a
/// handwriting face. Shown through an <c>&lt;img&gt;</c>, an SVG cannot run script, and only a value in the
/// shape these write is ever shown at all.
/// </remarks>
public static class BormSignature
{
    /// <summary>What every signature value starts with.</summary>
    public const string DataUrlPrefix = "data:image/svg+xml;base64,";

    // A signature of every point a long scribble produces is a few tens of kilobytes; anything larger than
    // this did not come from a signature pad.
    private const int MaxLength = 512 * 1024;

    private const double Padding = 8;
    private const string Ink = "#1a1a1a";

    // The two images FromStrokes and FromTypedName write, and nothing else: a path of moves and lines, or a name
    // whose markup is escaped. An SVG embedded in a page rather than shown through an <img> runs what it holds,
    // so a value that only starts like one - posted by anyone who can post the form - is not taken for one.
    private static readonly Regex Drawn = new(
        @"\A<svg xmlns='http://www\.w3\.org/2000/svg' viewBox='0 0 [0-9.]+ [0-9.]+' width='[0-9.]+' height='[0-9.]+'>"
        + @"<path d='[ML0-9. ]*' fill='none' stroke='" + Ink + "' stroke-width='2\\.5' stroke-linecap='round' stroke-linejoin='round'/></svg>\\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    private static readonly Regex Typed = new(
        @"\A<svg xmlns='http://www\.w3\.org/2000/svg' viewBox='0 0 [0-9]+ 72' width='[0-9]+' height='72'>"
        + @"<text x='12' y='48' font-family='Segoe Script, Brush Script MT, Lucida Handwriting, cursive' font-size='34' fill='" + Ink + "'>"
        + @"(?:[^<>&""']|&(?:amp|lt|gt|quot|apos);)*</text></svg>\z",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    /// <summary>Whether <paramref name="value"/> is a signature image this library would show: one in the shape it writes.</summary>
    public static bool IsImage(string? value) => ToSvg(value) is not null;

    /// <summary>The signature drawn as <paramref name="strokes"/>, in the pad's coordinates; null when nothing was drawn.</summary>
    public static string? FromStrokes(IEnumerable<IReadOnlyList<(double X, double Y)>> strokes)
    {
        ArgumentNullException.ThrowIfNull(strokes);
        var drawn = strokes.Where(s => s.Count > 0).ToList();
        if (drawn.Count == 0) return null;

        var minX = drawn.Min(s => s.Min(p => p.X));
        var minY = drawn.Min(s => s.Min(p => p.Y));
        var maxX = drawn.Max(s => s.Max(p => p.X));
        var maxY = drawn.Max(s => s.Max(p => p.Y));

        var path = new StringBuilder();
        foreach (var stroke in drawn)
        {
            path.Append('M').Append(Number(stroke[0].X - minX + Padding)).Append(' ').Append(Number(stroke[0].Y - minY + Padding));
            // A tap is a dot: a path that goes nowhere draws nothing at all.
            if (stroke.Count == 1)
                path.Append('L').Append(Number(stroke[0].X - minX + Padding + 0.5)).Append(' ').Append(Number(stroke[0].Y - minY + Padding));
            for (var i = 1; i < stroke.Count; i++)
                path.Append('L').Append(Number(stroke[i].X - minX + Padding)).Append(' ').Append(Number(stroke[i].Y - minY + Padding));
        }

        var width = Number(maxX - minX + 2 * Padding);
        var height = Number(maxY - minY + 2 * Padding);
        return Encode($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} {height}' width='{width}' height='{height}'>"
                      + $"<path d='{path}' fill='none' stroke='{Ink}' stroke-width='2.5' stroke-linecap='round' stroke-linejoin='round'/></svg>");
    }

    /// <summary>A signature typed as <paramref name="name"/>, in a handwriting face; null for no name.</summary>
    public static string? FromTypedName(string? name)
    {
        var text = name?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length > 100) text = text[..100];

        var width = Math.Max(120, text.Length * 20 + 24);
        return Encode($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {width} 72' width='{width}' height='72'>"
                      + $"<text x='12' y='48' font-family='Segoe Script, Brush Script MT, Lucida Handwriting, cursive' font-size='34' fill='{Ink}'>"
                      + $"{SecurityElement.Escape(text)}</text></svg>");
    }

    /// <summary>
    /// The SVG markup of a signature value, or null when <paramref name="value"/> is not one - not in the shape
    /// <see cref="FromStrokes"/> or <see cref="FromTypedName"/> writes it, which is all markup safe to embed.
    /// </summary>
    public static string? ToSvg(string? value)
    {
        if (value is not { Length: > 0 and <= MaxLength } || !value.StartsWith(DataUrlPrefix, StringComparison.Ordinal)) return null;
        string svg;
        try
        {
            svg = Encoding.UTF8.GetString(Convert.FromBase64String(value[DataUrlPrefix.Length..]));
        }
        catch (FormatException)
        {
            return null;
        }
        return Drawn.IsMatch(svg) || Typed.IsMatch(svg) ? svg : null;
    }

    private static string Encode(string svg) => DataUrlPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));

    private static string Number(double value) => Math.Round(value, 1).ToString("0.#", CultureInfo.InvariantCulture);
}
