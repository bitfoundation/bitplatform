using System.Globalization;
using System.Text;

namespace Bit.Bmage;

/// <summary>
/// Turns a line of text into placed glyphs: picks a font for every character (falling back through the registered
/// fonts for one that has it), splits the line into runs of one font, script and direction, shapes each run with the
/// font's OpenType tables - Arabic joining forms and ligatures, contextual alternates, kerning, mark attachment - and
/// lays the runs out in the visual order the bidirectional algorithm gives.
/// </summary>
internal static class BmageTextShaper
{
    // Feature masks. Bit 0 is every feature that applies to the whole run.
    private const uint Global = 1, Isolated = 2, Final = 4, Medial = 8, Initial = 16;

    private static readonly (uint, uint)[] ArabicSubstitutions =
    [
        (BmageOpenTypeLayout.Tag("ccmp"), Global), (BmageOpenTypeLayout.Tag("locl"), Global),
        (BmageOpenTypeLayout.Tag("isol"), Isolated), (BmageOpenTypeLayout.Tag("fina"), Final),
        (BmageOpenTypeLayout.Tag("medi"), Medial), (BmageOpenTypeLayout.Tag("init"), Initial),
        (BmageOpenTypeLayout.Tag("rlig"), Global), (BmageOpenTypeLayout.Tag("rclt"), Global), (BmageOpenTypeLayout.Tag("calt"), Global),
        (BmageOpenTypeLayout.Tag("liga"), Global), (BmageOpenTypeLayout.Tag("mset"), Global),
    ];

    private static readonly (uint, uint)[] Substitutions =
    [
        (BmageOpenTypeLayout.Tag("ccmp"), Global), (BmageOpenTypeLayout.Tag("locl"), Global), (BmageOpenTypeLayout.Tag("rlig"), Global),
        (BmageOpenTypeLayout.Tag("rclt"), Global), (BmageOpenTypeLayout.Tag("calt"), Global), (BmageOpenTypeLayout.Tag("liga"), Global),
        (BmageOpenTypeLayout.Tag("clig"), Global),
    ];

    private static readonly (uint, uint)[] Positioning =
    [
        (BmageOpenTypeLayout.Tag("kern"), Global), (BmageOpenTypeLayout.Tag("mark"), Global), (BmageOpenTypeLayout.Tag("mkmk"), Global),
        (BmageOpenTypeLayout.Tag("dist"), Global),
    ];

    /// <summary>The code points of a string.</summary>
    public static int[] CodePoints(string text)
    {
        var result = new List<int>(text.Length);
        foreach (var rune in text.EnumerateRunes()) result.Add(rune.Value);
        return [.. result];
    }

    /// <summary>
    /// Shapes and places the code points from <paramref name="start"/> to <paramref name="end"/> as one line, given
    /// their bidi levels for that line. <paramref name="fonts"/> is the order to look for glyphs in, the style's own
    /// font first.
    /// </summary>
    public static BmageShapedLine LayoutLine(IReadOnlyList<BmageFontFile> fonts, int[] codePoints, ReadOnlySpan<byte> levels, int start, int end, float fontSize, float letterSpacing)
    {
        var runs = Itemize(fonts, codePoints, levels, start, end);
        var primary = fonts[0];
        float ascent = primary.Ascender * fontSize / primary.UnitsPerEm, descent = -primary.Descender * fontSize / primary.UnitsPerEm;
        if (runs.Count == 0) return new BmageShapedLine([], 0, ascent, descent);

        // L2: runs in visual order, each drawn backwards when its level is odd.
        var runLevels = new byte[runs.Count];
        for (var r = 0; r < runs.Count; r++) runLevels[r] = runs[r].Level;
        var order = BmageBidiParagraph.GetVisualOrder(runLevels);

        var placed = new List<BmagePlacedGlyph>();
        var x = 0f;
        // The spacing after the last glyph that took any: it is not part of the visible width.
        var trailing = 0f;
        foreach (var r in order)
        {
            var run = runs[r];
            var font = run.Font;
            var scale = fontSize / font.UnitsPerEm;
            ascent = Math.Max(ascent, font.Ascender * scale);
            descent = Math.Max(descent, -font.Descender * scale);
            var buffer = ShapeRun(font, codePoints, run.Start, run.End, run.Script, rtl: (run.Level & 1) == 1);
            var glyphs = buffer.AsSpan();
            // Letters that join do not take letter spacing: it would break the joins.
            var spacing = run.Script == BmageUnicodeScript.Arabic ? 0f : letterSpacing;

            // Pen positions first, then marks from the glyphs they hang on.
            var positions = new (float X, float Y)[glyphs.Length];
            var rtl = (run.Level & 1) == 1;
            for (var v = 0; v < glyphs.Length; v++)
            {
                var i = rtl ? glyphs.Length - 1 - v : v;
                ref var g = ref glyphs[i];
                if (g.AttachTo >= 0) continue;
                positions[i] = (x + g.XOffset * scale, -g.YOffset * scale);
                if (g.Hidden) continue;
                x += g.XAdvance * scale;
                if (g.XAdvance == 0) continue;
                x += spacing;
                trailing = spacing;
            }
            for (var i = 0; i < glyphs.Length; i++)
            {
                if (glyphs[i].AttachTo >= 0) positions[i] = MarkPosition(glyphs, positions, i, scale, 0);
            }
            for (var v = 0; v < glyphs.Length; v++)
            {
                var i = rtl ? glyphs.Length - 1 - v : v;
                if (glyphs[i].Hidden) continue;
                placed.Add(new BmagePlacedGlyph(font, glyphs[i].Glyph, positions[i].X, positions[i].Y));
            }
        }
        return new BmageShapedLine([.. placed], Math.Max(0, x - trailing), ascent, descent);
    }

    /// <summary>
    /// How much each code point adds to the width of the line, shaped as a whole: a ligature's or a joined form's advance
    /// is on its first code point, a mark's on nothing. A wrap measures its candidate lines as sums of these instead of
    /// shaping every candidate on its own, which the breaks it then settles on are shaped as.
    /// </summary>
    public static float[] MeasureAdvances(IReadOnlyList<BmageFontFile> fonts, int[] codePoints, ReadOnlySpan<byte> levels, int start, int end, float fontSize, float letterSpacing)
    {
        var advances = new float[codePoints.Length];
        foreach (var run in Itemize(fonts, codePoints, levels, start, end))
        {
            var scale = fontSize / run.Font.UnitsPerEm;
            var spacing = run.Script == BmageUnicodeScript.Arabic ? 0f : letterSpacing;
            var glyphs = ShapeRun(run.Font, codePoints, run.Start, run.End, run.Script, rtl: (run.Level & 1) == 1).AsSpan();
            for (var i = 0; i < glyphs.Length; i++)
            {
                ref var g = ref glyphs[i];
                if (g.AttachTo >= 0 || g.Hidden || g.XAdvance == 0) continue;
                advances[Math.Clamp(g.Cluster, run.Start, run.End - 1)] += g.XAdvance * scale + spacing;
            }
        }
        return advances;
    }

    private static (float X, float Y) MarkPosition(Span<BmageShapingGlyph> glyphs, (float X, float Y)[] positions, int i, float scale, int depth)
    {
        ref var g = ref glyphs[i];
        var target = g.AttachTo;
        var basePosition = depth < 16 && glyphs[target].AttachTo >= 0 && glyphs[target].AttachTo != i
            ? MarkPosition(glyphs, positions, target, scale, depth + 1)
            : positions[target];
        return (basePosition.X + (g.AttachDx + g.XOffset) * scale, basePosition.Y - (g.AttachDy + g.YOffset) * scale);
    }

    private readonly record struct Run(int Start, int End, BmageFontFile Font, BmageUnicodeScript Script, byte Level);

    /// <summary>Splits a line into runs that share a font, a script and a bidi level.</summary>
    private static List<Run> Itemize(IReadOnlyList<BmageFontFile> fonts, int[] codePoints, ReadOnlySpan<byte> levels, int start, int end)
    {
        var runs = new List<Run>();
        var count = end - start;
        if (count <= 0) return runs;
        var chosenFonts = new BmageFontFile[count];
        var scripts = new BmageUnicodeScript[count];

        BmageFontFile? previous = null;
        var previousScript = BmageUnicodeScript.Common;
        for (var k = 0; k < count; k++)
        {
            var cp = codePoints[start + k];
            var script = BmageUnicodeData.GetScript(cp);
            var inherits = script is BmageUnicodeScript.Common or BmageUnicodeScript.Inherited;
            if (inherits && previous is null)
            {
                // Leading punctuation or spaces go with the text they open.
                for (var n = k + 1; n < count; n++)
                {
                    var next = codePoints[start + n];
                    var nextScript = BmageUnicodeData.GetScript(next);
                    if (nextScript is BmageUnicodeScript.Common or BmageUnicodeScript.Inherited) continue;
                    var nextFont = FirstWithGlyph(fonts, next);
                    if (nextFont is not null && (nextFont.HasGlyph(cp) || IsInvisible(cp))) previous = nextFont;
                    previousScript = nextScript;
                    break;
                }
            }
            BmageFontFile font;
            if (previous is not null && (IsInvisible(cp) || IsMark(cp) || (inherits && previous.HasGlyph(cp))))
                font = previous;
            else
                font = FirstWithGlyph(fonts, cp) ?? previous ?? fonts[0];
            chosenFonts[k] = font;
            scripts[k] = inherits ? previousScript : script;
            previous = font;
            if (!inherits) previousScript = script;
        }

        var runStart = 0;
        for (var k = 1; k <= count; k++)
        {
            if (k < count && chosenFonts[k] == chosenFonts[runStart] && scripts[k] == scripts[runStart] && levels[k] == levels[runStart]) continue;
            runs.Add(new Run(start + runStart, start + k, chosenFonts[runStart], scripts[runStart], levels[runStart]));
            runStart = k;
        }
        return runs;
    }

    private static BmageFontFile? FirstWithGlyph(IReadOnlyList<BmageFontFile> fonts, int codePoint)
    {
        foreach (var font in fonts)
            if (font.HasGlyph(codePoint)) return font;
        return null;
    }

    private static bool IsInvisible(int codePoint) => BmageUnicodeData.IsDefaultIgnorable(codePoint);

    private static bool IsMark(int codePoint) => Rune.IsValid(codePoint)
        && Rune.GetUnicodeCategory(new Rune(codePoint)) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.SpacingCombiningMark;

    private static uint ScriptTag(BmageUnicodeScript script) => script switch
    {
        BmageUnicodeScript.Arabic => BmageOpenTypeLayout.Tag("arab"),
        BmageUnicodeScript.Hebrew => BmageOpenTypeLayout.Tag("hebr"),
        BmageUnicodeScript.Greek => BmageOpenTypeLayout.Tag("grek"),
        BmageUnicodeScript.Cyrillic => BmageOpenTypeLayout.Tag("cyrl"),
        BmageUnicodeScript.Latin => BmageOpenTypeLayout.Tag("latn"),
        _ => BmageOpenTypeLayout.Tag("DFLT"),
    };

    /// <summary>Shapes one run in logical order: glyphs, substitutions, then positions in font units.</summary>
    internal static BmageGlyphBuffer ShapeRun(BmageFontFile font, int[] codePoints, int start, int end, BmageUnicodeScript script, bool rtl)
    {
        var layout = font.Layout;
        var buffer = new BmageGlyphBuffer();
        for (var i = start; i < end; i++)
        {
            var cp = codePoints[i];
            // L4: a mirrored character drawn right to left uses its mirror, such as ) for (.
            if (rtl && BmageUnicodeData.GetMirror(cp) is var mirror and >= 0 && font.HasGlyph(mirror)) cp = mirror;
            var glyph = font.GetGlyphIndex(cp);
            var g = new BmageShapingGlyph
            {
                Glyph = glyph,
                Cluster = i,
                Mask = Global,
                // As browsers do: ZWNJ, ZWJ, the bidi marks and a soft hyphen shape the text around them but are never drawn.
                Hidden = IsInvisible(cp),
                AttachTo = -1,
            };
            layout.Classify(ref g);
            buffer.Add(g);
        }

        if (script == BmageUnicodeScript.Arabic) SetJoiningForms(buffer, codePoints);

        var tag = ScriptTag(script);
        layout.Substitute(buffer, tag, script == BmageUnicodeScript.Arabic ? ArabicSubstitutions : Substitutions);

        var glyphs = buffer.AsSpan();
        for (var i = 0; i < glyphs.Length; i++)
        {
            glyphs[i].XAdvance = glyphs[i].Hidden ? 0 : font.GetAdvanceWidth(glyphs[i].Glyph);
            glyphs[i].AttachTo = -1;
        }

        if (layout.HasPositioning)
        {
            layout.Position(buffer, tag, Positioning);
        }
        else if (font.HasKerning)
        {
            // A font with only the legacy kern table: pairs of neighbouring glyphs.
            for (var i = 0; i + 1 < glyphs.Length; i++) glyphs[i].XAdvance += font.GetKerning(glyphs[i].Glyph, glyphs[i + 1].Glyph);
        }

        // Marks take no room of their own: they hang on the glyph before them.
        glyphs = buffer.AsSpan();
        for (var i = 0; i < glyphs.Length; i++)
            if (glyphs[i].GlyphClass == 3) glyphs[i].XAdvance = 0;
        return buffer;
    }

    /// <summary>
    /// Chooses each Arabic letter's form from how it joins its neighbours: initial, medial, final or isolated, skipping
    /// the transparent harakat between letters.
    /// </summary>
    private static void SetJoiningForms(BmageGlyphBuffer buffer, int[] codePoints)
    {
        var glyphs = buffer.AsSpan();
        var types = new BmageJoiningType[glyphs.Length];
        for (var i = 0; i < glyphs.Length; i++) types[i] = BmageUnicodeData.GetJoiningType(codePoints[glyphs[i].Cluster]);

        var joinsPrevious = new bool[glyphs.Length];
        var joinsNext = new bool[glyphs.Length];
        var previous = -1;
        for (var i = 0; i < glyphs.Length; i++)
        {
            if (types[i] == BmageJoiningType.T) continue;
            if (previous >= 0 && types[previous] is BmageJoiningType.D or BmageJoiningType.L or BmageJoiningType.C && types[i] is BmageJoiningType.D or BmageJoiningType.R or BmageJoiningType.C)
            {
                joinsNext[previous] = true;
                joinsPrevious[i] = true;
            }
            previous = i;
        }

        for (var i = 0; i < glyphs.Length; i++)
        {
            var form = types[i] switch
            {
                BmageJoiningType.D or BmageJoiningType.C => joinsPrevious[i] && joinsNext[i] ? Medial : joinsPrevious[i] ? Final : joinsNext[i] ? Initial : Isolated,
                BmageJoiningType.R => joinsPrevious[i] ? Final : Isolated,
                BmageJoiningType.L => joinsNext[i] ? Initial : Isolated,
                _ => 0u,
            };
            glyphs[i].Mask |= form;
        }
    }
}
