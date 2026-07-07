using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>Parses GitHub-style pipe tables (a header row followed by a delimiter row).</summary>
public sealed partial class BitMarkdownPipeTableBlockParser : BitMarkdownBlockParser
{
    // Tables sit just before the paragraph fallback.
    public override int Order => 65;

    [GeneratedRegex(@"^\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?$")]
    private static partial Regex DelimiterRow();

    [GeneratedRegex(@"^:?-+:?$")]
    private static partial Regex AlignmentToken();

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        int i = state.Line;
        if (i + 1 >= lines.Count) return false;
        if (!lines[i].Contains('|') || !IsDelimiter(lines[i + 1])) return false;

        var header = SplitRow(lines[i]);
        var delims = SplitRow(lines[i + 1]);
        if (header.Count == 0 || delims.Count != header.Count) return false;

        var alignments = new List<BitMarkdownColumnAlignment>();
        foreach (var d in delims)
        {
            string t = d.Trim();
            if (!AlignmentToken().IsMatch(t)) return false;
            bool l = t.StartsWith(':'), r = t.EndsWith(':');
            alignments.Add((l, r) switch
            {
                (true, true) => BitMarkdownColumnAlignment.Center,
                (true, false) => BitMarkdownColumnAlignment.Left,
                (false, true) => BitMarkdownColumnAlignment.Right,
                _ => BitMarkdownColumnAlignment.None
            });
        }

        var table = new BitMarkdownTableNode();
        table.Alignments.AddRange(alignments);
        foreach (var cell in header)
            table.Header.Add(state.ParseInlines(cell.Trim()));

        int j = i + 2;
        while (j < lines.Count && !BitMarkdownBlockProcessor.IsBlank(lines[j])
               && lines[j].Contains('|') && !state.StartsBlock(j))
        {
            var cells = SplitRow(lines[j]);
            var row = new List<List<BitMarkdownNode>>();
            for (int c = 0; c < header.Count; c++)
                row.Add(state.ParseInlines(c < cells.Count ? cells[c].Trim() : string.Empty));
            table.Rows.Add(row);
            j++;
        }

        output.Add(table);
        state.Line = j;
        return true;
    }

    private static bool IsDelimiter(string line)
    {
        string s = line.Trim();
        return s.Contains('-') && DelimiterRow().IsMatch(s);
    }

    private static List<string> SplitRow(string line)
    {
        string s = line.Trim();
        if (s.StartsWith('|')) s = s[1..];
        if (s.EndsWith('|') && !IsTrailingPipeEscaped(s)) s = s[..^1];

        var cells = new List<string>();
        var sb = new StringBuilder();
        int backtickRun = 0; // length of the backtick run that opened the current code span; 0 when outside.
        for (int i = 0; i < s.Length; i++)
        {
            // Handle backslash escapes as pairs so backslash parity is respected:
            // e.g. in "\\|" the first backslash escapes the second, leaving the pipe
            // as a real cell delimiter, whereas in "\|" the pipe is escaped.
            if (s[i] == '\\' && i + 1 < s.Length && backtickRun == 0)
            {
                char next = s[i + 1];
                if (next == '|') sb.Append('|');                            // escaped pipe -> literal '|'
                else if (next == '`') { sb.Append('\\'); sb.Append('`'); }  // preserve escape so inline parsing handles it
                else { sb.Append('\\'); sb.Append(next); }                  // consume the pair (e.g. "\\")
                i++;
            }
            else if (s[i] == '`')
            {
                int runStart = i;
                while (i + 1 < s.Length && s[i + 1] == '`') i++;
                int runLength = i - runStart + 1;
                // Only enter code-span mode if a matching closing run of the same length
                // exists ahead. An unmatched backtick run is literal text (CommonMark), so
                // later pipes must still be treated as cell delimiters rather than content.
                if (backtickRun == 0)
                {
                    if (HasMatchingBacktickRun(s, i + 1, runLength)) backtickRun = runLength;
                }
                else if (runLength == backtickRun) backtickRun = 0;
                sb.Append(s, runStart, runLength);
            }
            else if (s[i] == '|' && backtickRun == 0) { cells.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(s[i]);
        }
        cells.Add(sb.ToString());
        return cells;
    }

    // Scans from startIndex for a backtick run of exactly the given length, used to
    // decide whether an opening backtick run has a valid closing run (a code span's
    // closing run must contain the same number of backticks as the opening run).
    private static bool HasMatchingBacktickRun(string s, int startIndex, int runLength)
    {
        for (int i = startIndex; i < s.Length; i++)
        {
            if (s[i] != '`') continue;
            int runStart = i;
            while (i + 1 < s.Length && s[i + 1] == '`') i++;
            if (i - runStart + 1 == runLength) return true;
        }
        return false;
    }

    // Determines whether the final '|' of a row is backslash-escaped by counting
    // the consecutive backslashes immediately preceding it. An odd count means the
    // pipe is escaped (and must be kept); an even count (including zero) means it is
    // a real trailing delimiter that should be trimmed.
    private static bool IsTrailingPipeEscaped(string s)
    {
        int backslashes = 0;
        for (int i = s.Length - 2; i >= 0 && s[i] == '\\'; i--) backslashes++;
        return (backslashes & 1) == 1;
    }
}
