using System.Globalization;

namespace Bit.Bmotion;

/// <summary>
/// One piece of a split text run. A chunk is either a <see cref="IsGap"/> run of whitespace
/// (rendered verbatim, never animated, so the text still wraps and reads normally) or a group of
/// one or more animated <see cref="Units"/>.
/// </summary>
/// <param name="Text">The chunk's raw text - the whitespace itself for a gap, the word/line otherwise.</param>
/// <param name="IsGap">Whether this chunk is inter-unit whitespace rather than animated content.</param>
/// <param name="Units">The individually animated units of this chunk (empty for a gap).</param>
internal sealed record BmTextChunk(string Text, bool IsGap, IReadOnlyList<string> Units);

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
    public static List<BmTextChunk> Split(string? text, BmSplitBy by)
    {
        var chunks = new List<BmTextChunk>();
        if (string.IsNullOrEmpty(text)) return chunks;

        if (by == BmSplitBy.Lines)
        {
            // Authored lines: split on \n, tolerating \r\n. Empty lines are kept so blank spacing
            // in the source text survives, but they carry no animated unit of their own.
            foreach (var line in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                chunks.Add(line.Length == 0
                    ? new BmTextChunk(string.Empty, IsGap: true, Array.Empty<string>())
                    : new BmTextChunk(line, IsGap: false, new[] { line }));
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
                chunks.Add(new BmTextChunk(run, IsGap: true, Array.Empty<string>()));
            }
            else
            {
                var units = by == BmSplitBy.Words ? new[] { run } : Graphemes(run);
                chunks.Add(new BmTextChunk(run, IsGap: false, units));
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
    public static int CountUnits(List<BmTextChunk> chunks)
    {
        int total = 0;
        foreach (var chunk in chunks) total += chunk.Units.Count;
        return total;
    }
}
