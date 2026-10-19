using System.Globalization;
using System.Numerics;
using Bit.Bmage.Drawing;

namespace Bit.Bmage.Text;

/// <summary>
/// Turns text into outlines using fonts loaded by this library. Works anywhere .NET runs, including headless server code,
/// with no browser involvement. Register fonts by family name and let the style pick one.
/// </summary>
/// <remarks>
/// Layout is left-to-right with kerning and optional word wrapping. Complex script shaping (Arabic joining, Indic
/// reordering) and bidirectional reordering are not performed; for those, use the browser-backed rasteriser from the
/// Blazor package, which delegates to the platform's text engine.
/// </remarks>
public sealed class FontTextRasterizer : ITextRasterizer
{
    private readonly Dictionary<string, FontVariants> _families = new(StringComparer.OrdinalIgnoreCase);
    private FontFile? _fallback;

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

    /// <summary>Registers a font under a family name. The first font registered also becomes the fallback.</summary>
    public FontTextRasterizer Register(string familyName, FontFile font, bool bold = false, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(familyName);
        ArgumentNullException.ThrowIfNull(font);
        if (!_families.TryGetValue(familyName, out var variants))
            _families[familyName] = variants = new FontVariants();
        variants.Set(font, bold, italic);
        _fallback ??= font;
        return this;
    }

    /// <summary>Registers a font using the family name recorded in the font itself.</summary>
    public FontTextRasterizer Register(FontFile font, bool bold = false, bool italic = false)
    {
        ArgumentNullException.ThrowIfNull(font);
        return Register(font.FamilyName ?? font.FullName ?? "default", font, bold, italic);
    }

    /// <summary>Sets the font used when a requested family is not registered.</summary>
    public FontTextRasterizer SetFallback(FontFile font)
    {
        _fallback = font ?? throw new ArgumentNullException(nameof(font));
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
        return _fallback;
    }

    /// <inheritdoc />
    public TextMetrics Measure(string text, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var font = ResolveFont(style);
        if (font is null || string.IsNullOrEmpty(text)) return new TextMetrics(0, 0, 0, 0, 0);
        var layout = Layout(text, style, font);
        return layout.Metrics;
    }

    /// <inheritdoc />
    public VectorPath? GetTextPath(string text, Vector2 origin, TextStyle style)
    {
        ArgumentNullException.ThrowIfNull(style);
        var font = ResolveFont(style)
            ?? throw new ImageCapabilityException("No font is registered. Call Register with a TrueType font, or use a browser-backed text rasteriser.");
        if (string.IsNullOrEmpty(text)) return null;

        var layout = Layout(text, style, font);
        if (layout.Lines.Count == 0) return null;
        var scale = style.FontSize / font.UnitsPerEm;
        var builder = new PathBuilder { FlatteningTolerance = 0.15f };

        var originY = origin.Y + style.Baseline switch
        {
            TextBaseline.Top => font.Ascender * scale,
            TextBaseline.Middle => (font.Ascender * scale) - layout.Metrics.Height / 2,
            TextBaseline.Bottom => -(layout.Metrics.Height - font.Ascender * scale),
            _ => 0f,
        };

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
                var outline = font.GetGlyphOutline(g.GlyphIndex);
                outline?.AppendTo(builder, new Vector2(x + g.Offset, y), scale);
            }
        }
        var path = builder.Build();
        return path.IsEmpty ? null : path;
    }

    private readonly record struct PositionedGlyph(ushort GlyphIndex, float Offset);

    private sealed class Line
    {
        public List<PositionedGlyph> Glyphs { get; } = [];
        public float Width;
    }

    private readonly record struct LayoutResult(List<Line> Lines, TextMetrics Metrics);

    private static LayoutResult Layout(string text, TextStyle style, FontFile font)
    {
        var scale = style.FontSize / font.UnitsPerEm;
        var lines = new List<Line>();
        var maxWidth = style.MaxWidth;

        foreach (var rawLine in SplitLines(text))
        {
            if (maxWidth is not { } limit || limit <= 0)
            {
                lines.Add(BuildLine(rawLine, font, scale, style.LetterSpacing));
                continue;
            }
            // Greedy word wrapping; a single word longer than the limit is broken by character.
            var current = string.Empty;
            foreach (var word in SplitWords(rawLine))
            {
                var candidate = current + word;
                if (current.Length == 0 || BuildLine(candidate.TrimEnd(), font, scale, style.LetterSpacing).Width <= limit)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(BuildLine(current.TrimEnd(), font, scale, style.LetterSpacing));
                    current = word.TrimStart();
                }

                // Only a lone word can be too wide here, whether it started the text or was just wrapped: break it,
                // keeping its trailing space so the next word is not glued to it.
                var trimmed = current.TrimEnd();
                if (trimmed.Length > 0 && BuildLine(trimmed, font, scale, style.LetterSpacing).Width > limit)
                {
                    var broken = BreakWord(trimmed, font, scale, style.LetterSpacing, limit);
                    if (broken.Count == 0) continue;
                    for (var i = 0; i < broken.Count - 1; i++) lines.Add(BuildLine(broken[i], font, scale, style.LetterSpacing));
                    current = broken[^1] + current[trimmed.Length..];
                }
            }
            lines.Add(BuildLine(current.TrimEnd(), font, scale, style.LetterSpacing));
        }

        var width = lines.Count == 0 ? 0 : lines.Max(l => l.Width);
        var ascent = font.Ascender * scale;
        var descent = -font.Descender * scale;
        var height = lines.Count == 0 ? 0 : (lines.Count - 1) * style.FontSize * style.LineHeight + ascent + descent;
        return new LayoutResult(lines, new TextMetrics(width, height, ascent, descent, lines.Count));
    }

    private static List<string> BreakWord(string word, FontFile font, float scale, float letterSpacing, float limit)
    {
        var parts = new List<string>();
        var current = string.Empty;
        foreach (var element in EnumerateTextElements(word))
        {
            var candidate = current + element;
            if (current.Length > 0 && BuildLine(candidate, font, scale, letterSpacing).Width > limit)
            {
                parts.Add(current);
                current = element;
            }
            else current = candidate;
        }
        parts.Add(current);
        return parts;
    }

    private static IEnumerable<string> EnumerateTextElements(string s)
    {
        var e = StringInfo.GetTextElementEnumerator(s);
        while (e.MoveNext()) yield return (string)e.Current;
    }

    private static Line BuildLine(string text, FontFile font, float scale, float letterSpacing)
    {
        var line = new Line();
        var x = 0f;
        ushort previous = 0;
        var first = true;
        foreach (var rune in text.EnumerateRunes())
        {
            var glyph = font.GetGlyphIndex(rune.Value);
            if (!first && font.HasKerning) x += font.GetKerning(previous, glyph) * scale;
            line.Glyphs.Add(new PositionedGlyph(glyph, x));
            x += font.GetAdvanceWidth(glyph) * scale + letterSpacing;
            previous = glyph;
            first = false;
        }
        // Trailing letter spacing is not part of the visible width.
        line.Width = Math.Max(0, x - (line.Glyphs.Count > 0 ? letterSpacing : 0));
        return line;
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

    /// <summary>Splits into words keeping the trailing whitespace with each word, so wrapping preserves spacing.</summary>
    private static IEnumerable<string> SplitWords(string line)
    {
        var start = 0;
        var i = 0;
        while (i < line.Length)
        {
            while (i < line.Length && !char.IsWhiteSpace(line[i])) i++;
            while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
            yield return line[start..i];
            start = i;
        }
        if (start == 0 && line.Length == 0) yield return string.Empty;
    }
}
