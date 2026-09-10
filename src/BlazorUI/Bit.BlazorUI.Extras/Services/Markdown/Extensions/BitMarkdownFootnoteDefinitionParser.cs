namespace Bit.BlazorUI;

/// <summary>
/// Parses GitHub-style footnote definitions: <c>[^label]: the note</c>, optionally continued
/// on following lines indented to line up under the note's text.
/// </summary>
public sealed class BitMarkdownFootnoteDefinitionParser : BitMarkdownBlockParser
{
    // Claims its lines before the link reference definition parser (5), which would
    // otherwise read "[^1]: ..." as an ordinary reference definition.
    public override int Order => 4;

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        int i = state.Line;
        string line = lines[i];

        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;

        int p = 0;
        while (p < line.Length && line[p] == ' ') p++;
        if (p + 1 >= line.Length || line[p] != '[' || line[p + 1] != '^') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(line, p);
        if (labelEnd < 0) return false;
        if (labelEnd + 1 >= line.Length || line[labelEnd + 1] != ':') return false;

        // The length limit is on the label as written, so whitespace a normalized label
        // collapses away cannot smuggle an over-long one through.
        string raw = line.Substring(p + 1, labelEnd - p - 1);
        if (raw.Length > BitMarkdownLinkHelpers.MaxLabelLength) return false;

        string label = BitMarkdownLinkHelpers.NormalizeLabel(raw);
        // "^" alone is not a label.
        if (label.Length <= 1 || label.Length > BitMarkdownLinkHelpers.MaxLabelLength) return false;

        var content = new List<string> { line[(labelEnd + 2)..].TrimStart(' ', '\t') };
        i++;

        // Continuation lines belong to the note while they are indented (or blank and
        // followed by an indented line), mirroring how a list item is continued.
        while (i < lines.Count)
        {
            string next = lines[i];
            if (BitMarkdownBlockProcessor.IsBlank(next))
            {
                int j = i + 1;
                while (j < lines.Count && BitMarkdownBlockProcessor.IsBlank(lines[j])) j++;
                if (j < lines.Count && BitMarkdownBlockProcessor.GetIndent(lines[j]) >= 4)
                {
                    content.Add(string.Empty);
                    i++;
                    continue;
                }
                break;
            }

            if (BitMarkdownBlockProcessor.GetIndent(next) < 4) break;

            content.Add(BitMarkdownBlockProcessor.StripIndent(next, 4));
            i++;
        }

        var definition = new BitMarkdownFootnoteDefinitionNode { Label = label };
        definition.Children.AddRange(state.ParseBlocks(content));
        output.Add(definition);
        state.Line = i;
        return true;
    }
}
