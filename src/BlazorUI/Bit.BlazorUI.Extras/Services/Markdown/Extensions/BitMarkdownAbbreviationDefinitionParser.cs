namespace Bit.BlazorUI;

/// <summary>
/// Parses abbreviation definitions: <c>*[HTML]: HyperText Markup Language</c>, written anywhere in
/// the document.
/// </summary>
public sealed class BitMarkdownAbbreviationDefinitionParser : BitMarkdownBlockParser
{
    // Claimed before the link reference definition parser (5) and long before the list parser,
    // whose "*" marker the line also starts with.
    public override int Order => 3;

    // A definition ends the paragraph above it, the way markdown-it's own abbreviation rule does.
    // Written on the line right after a sentence - the natural place to explain a term just used -
    // it would otherwise be absorbed into that paragraph and printed as raw text, expanding nothing.
    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => TryReadDefinition(state.Lines[lineIndex], out _, out _);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        if (TryReadDefinition(state.Lines[state.Line], out string label, out string title) is false) return false;

        output.Add(new BitMarkdownAbbreviationDefinitionNode
        {
            Label = label,
            Title = BitMarkdownEntities.Decode(title)
        });
        state.Line++;
        return true;
    }

    // Reads a "*[LABEL]: expansion" line. One reader for both the acceptance test and the parse, so
    // the line that ends a paragraph and the line that becomes a definition cannot come apart.
    private static bool TryReadDefinition(string line, out string label, out string title)
    {
        label = string.Empty;
        title = string.Empty;

        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;

        int p = 0;
        while (p < line.Length && line[p] == ' ') p++;
        if (p + 1 >= line.Length || line[p] != '*' || line[p + 1] != '[') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(line, p + 1);
        if (labelEnd < 0) return false;
        if (labelEnd + 1 >= line.Length || line[labelEnd + 1] != ':') return false;

        string read = line.Substring(p + 2, labelEnd - p - 2).Trim();
        if (read.Length is 0 or > BitMarkdownLinkHelpers.MaxLabelLength) return false;

        string expansion = line[(labelEnd + 2)..].Trim();
        // A definition with nothing on the right explains nothing; it is ordinary text.
        if (expansion.Length == 0) return false;

        label = read;
        title = expansion;
        return true;
    }
}
