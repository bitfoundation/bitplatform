using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Parses a display-math block: <c>$$</c> on a line of its own, the TeX, then a closing
/// <c>$$</c>. The content is taken verbatim, exactly as a fenced code block's is.
/// </summary>
public sealed class BitMarkdownMathBlockParser : BitMarkdownBlockParser
{
    // Beside the fenced code block parser (10): both are verbatim fences, and neither may be
    // reinterpreted by anything that runs later.
    public override int Order => 12;

    // Only a line that actually opens a block ends the paragraph before it. A "$$" that is neither
    // a complete one-line block nor closed further down is prose - "The cost is\n$$5 for two" is
    // one paragraph with a soft break in it, not two paragraphs split around a fence that never was.
    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => Opens(state.Lines, lineIndex);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        string first = lines[state.Line];
        if (IsFence(first) is false) return false;

        // "$$ x = 1 $$" on one line is a complete block.
        string opening = first.Trim();
        if (opening.Length > 4 && opening.EndsWith("$$", StringComparison.Ordinal))
        {
            output.Add(new BitMarkdownMathNode { Content = opening[2..^2].Trim(), Display = true, Block = true });
            state.Line++;
            return true;
        }
        if (opening.Length != 2) return false;

        var sb = new StringBuilder();
        int i = state.Line + 1;
        bool closed = false;
        while (i < lines.Count)
        {
            if (lines[i].Trim() == "$$") { i++; closed = true; break; }
            sb.Append(lines[i]).Append('\n');
            i++;
        }

        // An unterminated fence is not a block; leaving it unmatched keeps the document rendering
        // as it did before the flavor was enabled.
        if (closed is false) return false;

        output.Add(new BitMarkdownMathNode
        {
            Content = BitMarkdownBlockProcessor.TrimTrailingNewline(sb.ToString()),
            Display = true,
            Block = true
        });
        state.Line = i;
        return true;
    }

    // The same acceptance TryParse applies, asked of a line without consuming it: a complete
    // one-line block, or an opening "$$" with a closing one somewhere below it.
    private static bool Opens(IReadOnlyList<string> lines, int index)
    {
        if (IsFence(lines[index]) is false) return false;

        string opening = lines[index].Trim();
        if (opening.Length > 4 && opening.EndsWith("$$", StringComparison.Ordinal)) return true;
        if (opening.Length != 2) return false;

        for (int i = index + 1; i < lines.Count; i++)
        {
            if (lines[i].Trim() == "$$") return true;
        }
        return false;
    }

    private static bool IsFence(string line)
    {
        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;

        string trimmed = line.Trim();
        return trimmed.Length >= 2 && trimmed.StartsWith("$$", StringComparison.Ordinal);
    }
}
