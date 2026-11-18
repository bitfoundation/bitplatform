using System.Globalization;

namespace Bit.Bmotion;

/// <summary>
/// Splits a text run into the chunks <see cref="BmotionSplitText"/> renders. Pure string work with
/// no rendering concerns, so the splitting rules are unit-testable on their own.
/// </summary>
internal static class BmotionTextSplitter
{
    /// <summary>
    /// Splits <paramref name="text"/> into chunks according to <paramref name="by"/>. Whitespace
    /// between words is preserved as gap chunks so the rendered text still wraps, collapses and
    /// copies like the original.
    /// </summary>
    public static List<BmotionTextChunk> Split(string? text, BmotionSplitBy by)
    {
        var chunks = new List<BmotionTextChunk>();
        if (string.IsNullOrEmpty(text)) return chunks;

        if (by == BmotionSplitBy.Lines)
        {
            // Authored lines: split on \n, tolerating \r\n. Empty lines are kept so blank spacing
            // in the source text survives, but they carry no animated unit of their own.
            foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                chunks.Add(line.Length == 0
                    ? new BmotionTextChunk(string.Empty, IsGap: true, Array.Empty<string>())
                    : new BmotionTextChunk(line, IsGap: false, new[] { line }));
            }
            return chunks;
        }

        // Words / Chars: walk the string, alternating between whitespace runs (gaps) and
        // non-whitespace runs (words).
        int i = 0;
        while (i < text.Length)
        {
            int start = i;
            bool gap = char.IsWhiteSpace(text[i]);
            while (i < text.Length && char.IsWhiteSpace(text[i]) == gap) i++;
            var run = text[start..i];

            if (gap)
            {
                chunks.Add(new BmotionTextChunk(run, IsGap: true, Array.Empty<string>()));
            }
            else
            {
                var units = by == BmotionSplitBy.Words ? new[] { run } : Graphemes(run);
                chunks.Add(new BmotionTextChunk(run, IsGap: false, units));
            }
        }
        return chunks;
    }

    /// <summary>
    /// Splits a word into user-perceived characters. Grapheme clusters rather than UTF-16 code
    /// units, so surrogate pairs, emoji sequences and combining marks animate as one character
    /// instead of being torn into unrenderable halves.
    /// </summary>
    private static string[] Graphemes(string word)
    {
        var result = new List<string>(word.Length);
        var enumerator = StringInfo.GetTextElementEnumerator(word);
        while (enumerator.MoveNext()) result.Add((string)enumerator.Current);
        return result.ToArray();
    }

    /// <summary>The number of animated units across every chunk (the stagger's <c>total</c>).</summary>
    public static int CountUnits(List<BmotionTextChunk> chunks)
    {
        int total = 0;
        foreach (var chunk in chunks) total += chunk.Units.Count;
        return total;
    }
}
