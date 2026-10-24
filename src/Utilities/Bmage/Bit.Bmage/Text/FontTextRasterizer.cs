using System.Globalization;
using System.Numerics;
using System.Text;
using Bit.Bmage.Drawing;
using Bit.Bmage.Text.Unicode;

namespace Bit.Bmage.Text;

/// <summary>
/// Turns text into outlines using fonts loaded by this library. Works anywhere .NET runs, including headless server code,
/// with no browser involvement. Register fonts by family name and let the style pick one.
/// </summary>
/// <remarks>
/// <para>
/// Text is shaped with each font's OpenType tables: Arabic and Persian letters take their joining forms and
/// ligatures, contextual alternates, kerning and mark positioning apply, and mixed right-to-left and left-to-right
/// text is ordered by the Unicode Bidirectional Algorithm. A character the style's font lacks is drawn with the first
/// other registered font that has it, so registering a Latin font and an Arabic one covers text that mixes the two.
/// </para>
/// <para>
/// Scripts that reorder or stack glyphs within a word (Indic, Southeast Asian) and cursive attachment (Nastaliq) are
/// not shaped; for those, use the browser-backed rasteriser, which delegates to the platform's text engine.
/// </para>
/// </remarks>
public sealed class FontTextRasterizer : ITextRasterizer
{
    private readonly Dictionary<string, FontVariants> _families = new(StringComparer.OrdinalIgnoreCase);
    // The families in the order they were registered: the order a character missing from the style's font is looked for.
    private readonly List<FontVariants> _order = [];
    private FontFile? _fallback;
    // The family registered first, which answers a style that names no registered family with its own bold or italic
    // face - "sans-serif", the default, included. Null once SetFallback has chosen a single font.
    private FontVariants? _fallbackFamily;
    private readonly object _sync = new();
    // Laid out text by its content and style: the editor draws the same labels on every frame of a drag.
    private readonly Dictionary<LayoutKey, LayoutResult> _layouts = [];
    private const int MaxCachedLayouts = 128;

    private sealed class FontVariants
    {
        public FontFile? Regular, Bold, Italic, BoldItalic;

        public FontFile? Get(bool bold, bool italic) => (bold, italic) switch
        {
            (true, true) => BoldItalic ?? Bold ?? Italic ?? Regular,
            (true, false) => Bold ?? Regular ?? BoldItalic,
            (false, true) => Italic ?? Regular ?? BoldItalic,
            _ => Regular ?? Bold ?? Italic ?? BoldItalic,
        };

        public void Set(FontFile font, bool bold, bool italic)
        {
            if (bold && italic) BoldItalic = font;
            else if (bold) Bold = font;
            else if (italic) Italic = font;
            else Regular = font;
        }
    }

    /// <summary>
    /// Registers a font under a family name. The first family registered also becomes the fallback, with each of its
    /// faces: bold text in a family that is not registered is drawn with the fallback family's bold face. Characters
    /// a style's font lacks are looked for in the other families, in the order they were registered.
    /// </summary>
    public FontTextRasterizer Register(string familyName, FontFile font, bool bold = false, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(font);
        lock (_sync)
        {
            if (!_families.TryGetValue(familyName, out var variants))
            {
                _families[familyName] = variants = new FontVariants();
                _order.Add(variants);
            }
            variants.Set(font, bold, italic);
            if (_fallback is null)
            {
                _fallback = font;
                _fallbackFamily = variants;
            }
            _layouts.Clear();
        }
        return this;
    }

    /// <summary>Registers a font using the family name recorded in the font itself.</summary>
    public FontTextRasterizer Register(FontFile font, bool bold = false, bool italic = false)
    {
        ArgumentNullException.ThrowIfNull(font);
        return Register(font.FamilyName ?? font.FullName ?? "default", font, bold, italic);
    }

    /// <summary>Sets the font used, whatever the weight or style, when a requested family is not registered.</summary>
    public FontTextRasterizer SetFallback(FontFile font)
    {
        ArgumentNullException.ThrowIfNull(font);
        lock (_sync)
        {
            _fallback = font;
            _fallbackFamily = null;
            _layouts.Clear();
        }
        return this;
    }

    /// <summary>The family names that have been registered.</summary>
    public IReadOnlyCollection<string> Families => _families.Keys;

    /// <summary>True when at least one font is available.</summary>
    public bool HasFonts => _fallback is not null;

    /// <summary>
    /// Resolves the font for a style. The family may be a CSS style comma separated list; the first registered family
    /// wins, otherwise the fallback is used.
    /// </summary>
    public FontFile? ResolveFont(TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        foreach (var name in style.FontFamily.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var family = name.Trim('\'', '"');
            if (_families.TryGetValue(family, out var variants) && variants.Get(style.Bold, style.Italic) is { } font) return font;
        }
        return _fallbackFamily?.Get(style.Bold, style.Italic) ?? _fallback;
    }

    /// <inheritdoc />
    public TextMetrics Measure(string text, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var font = ResolveFont(style);
        if (font is null || string.IsNullOrEmpty(text)) return new TextMetrics(0, 0, 0, 0, 0);
        return GetLayout(text, style, font).Metrics;
    }

    /// <inheritdoc />
    public VectorPath? GetTextPath(string text, Vector2 origin, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var font = ResolveFont(style)
            ?? throw new ImageCapabilityException("No font is registered. Call Register with a TrueType font, or use a browser-backed text rasteriser.");
        if (string.IsNullOrEmpty(text)) return null;

        var layout = GetLayout(text, style, font);
        if (layout.Lines.Count == 0) return null;
        var builder = new PathBuilder { FlatteningTolerance = 0.15f };
        var metrics = layout.Metrics;

        var originY = origin.Y + style.Baseline switch
        {
            TextBaseline.Top => metrics.Ascent,
            TextBaseline.Middle => metrics.Ascent - metrics.Height / 2,
            TextBaseline.Bottom => -(metrics.Height - metrics.Ascent),
            _ => 0f,
        };

        // Glyph outlines are cached by their font, which is not safe to share between threads.
        lock (_sync)
        {
            for (var i = 0; i < layout.Lines.Count; i++)
            {
                var line = layout.Lines[i];
                var x = origin.X + style.Alignment switch
                {
                    TextAlignment.Center => -line.Width / 2,
                    TextAlignment.Right => -line.Width,
                    _ => 0f,
                };
                var y = originY + i * style.FontSize * style.LineHeight;
                foreach (var g in line.Glyphs)
                {
                    var outline = g.Font.GetGlyphOutline(g.Glyph);
                    outline?.AppendTo(builder, new Vector2(x + g.X, y + g.Y), style.FontSize / g.Font.UnitsPerEm);
                }
            }
        }
        var path = builder.Build();
        return path.IsEmpty ? null : path;
    }

    private readonly record struct LayoutKey(string Text, string Family, bool Bold, bool Italic, float Size, float? MaxWidth, float LetterSpacing, float LineHeight);

    private sealed record LayoutResult(List<ShapedLine> Lines, TextMetrics Metrics);

    private LayoutResult GetLayout(string text, TextStyle style, FontFile font)
    {
        var key = new LayoutKey(text, style.FontFamily, style.Bold, style.Italic, style.FontSize, style.MaxWidth, style.LetterSpacing, style.LineHeight);
        lock (_sync)
        {
            if (_layouts.TryGetValue(key, out var cached)) return cached;
            var layout = Layout(text, style, FallbackChain(font, style));
            if (_layouts.Count >= MaxCachedLayouts) _layouts.Clear();
            _layouts[key] = layout;
            return layout;
        }
    }

    /// <summary>The fonts to look for a character in: the style's own, then every registered family's matching face.</summary>
    private List<FontFile> FallbackChain(FontFile primary, TextStyle style)
    {
        var chain = new List<FontFile> { primary };
        foreach (var family in _order)
            if (family.Get(style.Bold, style.Italic) is { } face && !chain.Contains(face)) chain.Add(face);
        if (_fallback is { } fallback && !chain.Contains(fallback)) chain.Add(fallback);
        return chain;
    }

    private static LayoutResult Layout(string text, TextStyle style, List<FontFile> fonts)
    {
        var lines = new List<ShapedLine>();
        foreach (var paragraph in SplitLines(text))
        {
            // Each line of the text is a paragraph of its own, with its own direction (rules P2 and P3).
            var codePoints = TextShaper.CodePoints(paragraph);
            var bidi = BidiParagraph.Create(codePoints);
            ShapedLine Line(int start, int end) => TextShaper.LayoutLine(fonts, codePoints, bidi.GetLineLevels(start, end), start, end, style.FontSize, style.LetterSpacing);

            if (style.MaxWidth is not { } limit || limit <= 0)
            {
                lines.Add(Line(0, codePoints.Length));
                continue;
            }
            // The paragraph is shaped once to measure the candidate lines, then each line on its own as it is drawn.
            var advances = TextShaper.MeasureAdvances(fonts, codePoints, bidi.GetLineLevels(0, codePoints.Length), 0, codePoints.Length, style.FontSize, style.LetterSpacing);
            foreach (var (start, end) in Wrap(codePoints, limit, advances, (s, e) => Line(s, e).Width)) lines.Add(Line(start, end));
        }

        var width = lines.Count == 0 ? 0 : lines.Max(l => l.Width);
        var ascent = lines.Count == 0 ? 0 : lines.Max(l => l.Ascent);
        var descent = lines.Count == 0 ? 0 : lines.Max(l => l.Descent);
        var height = lines.Count == 0 ? 0 : (lines.Count - 1) * style.FontSize * style.LineHeight + ascent + descent;
        return new LayoutResult(lines, new TextMetrics(width, height, ascent, descent, lines.Count));
    }

    /// <summary>
    /// Greedy word wrapping of one paragraph into lines no wider than <paramref name="limit"/>. A candidate line is
    /// measured as the sum of its code points' <paramref name="advances"/>, from one shaping of the whole paragraph; a
    /// line about to be kept is measured as it will be drawn, with <paramref name="width"/>, and gives up its last words
    /// when its edges shape wider on their own. A line's trailing spaces do not count; a single word wider than the
    /// limit is broken between characters.
    /// </summary>
    private static List<(int Start, int End)> Wrap(int[] codePoints, float limit, float[] advances, Func<int, int, float> width)
    {
        var prefix = new float[codePoints.Length + 1];
        for (var i = 0; i < codePoints.Length; i++) prefix[i + 1] = prefix[i] + advances[i];
        float Estimate(int start, int end) => prefix[end] - prefix[start];

        var lines = new List<(int, int)>();
        // The words of the line being filled.
        var words = new List<(int Start, int End)>();
        var p = 0;
        while (true)
        {
            if (p < codePoints.Length)
            {
                var wordStart = p;
                while (p < codePoints.Length && !Rune.IsWhiteSpace(new Rune(codePoints[p]))) p++;
                var wordEnd = p;
                while (p < codePoints.Length && Rune.IsWhiteSpace(new Rune(codePoints[p]))) p++;
                if (words.Count == 0 || Estimate(words[0].Start, wordEnd) <= limit)
                {
                    words.Add((wordStart, wordEnd));
                    // Only a lone word can be too wide here, whether it started the text or was just wrapped: break it.
                    if (words.Count == 1 && Estimate(wordStart, wordEnd) > limit)
                    {
                        var pieces = BreakWord(codePoints, wordStart, wordEnd, limit, Estimate);
                        for (var k = 0; k < pieces.Count - 1; k++) lines.Add(pieces[k]);
                        words[0] = pieces[^1];
                    }
                    continue;
                }
                // The line is full: the word starts the next one.
                p = wordStart;
            }
            else if (words.Count == 0)
            {
                // Out of words; a paragraph without any is still a line.
                if (lines.Count == 0) lines.Add((0, 0));
                return lines;
            }
            // Shaped on its own the line may come out wider than its estimate, kerning and joining forms differing at
            // its edges; then its last words move down.
            var kept = words.Count;
            while (kept > 1 && width(words[0].Start, words[kept - 1].End) > limit) kept--;
            lines.Add((words[0].Start, words[kept - 1].End));
            if (kept < words.Count) p = words[kept].Start;
            words.Clear();
        }
    }

    /// <summary>Breaks a word between its grapheme clusters into pieces that fit, the last one possibly shorter.</summary>
    private static List<(int, int)> BreakWord(int[] codePoints, int start, int end, float limit, Func<int, int, float> width)
    {
        var text = new StringBuilder();
        for (var i = start; i < end; i++) text.Append(char.ConvertFromUtf32(codePoints[i]));
        var word = text.ToString();

        var pieces = new List<(int, int)>();
        var pieceStart = start;
        var position = start;
        for (var offset = 0; offset < word.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(word, offset);
            var runes = 0;
            foreach (var _ in word.AsSpan(offset, length).EnumerateRunes()) runes++;
            var next = position + runes;
            if (position > pieceStart && width(pieceStart, next) > limit)
            {
                pieces.Add((pieceStart, position));
                pieceStart = position;
            }
            position = next;
            offset += length;
        }
        pieces.Add((pieceStart, end));
        return pieces;
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                var end = i > start && text[i - 1] == '\r' ? i - 1 : i;
                yield return text[start..end];
                start = i + 1;
            }
        }
        yield return text[start..];
    }
}
