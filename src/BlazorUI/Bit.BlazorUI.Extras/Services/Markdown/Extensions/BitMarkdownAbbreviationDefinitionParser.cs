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

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        string line = lines[state.Line];

        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;

        int p = 0;
        while (p < line.Length && line[p] == ' ') p++;
        if (p + 1 >= line.Length || line[p] != '*' || line[p + 1] != '[') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(line, p + 1);
        if (labelEnd < 0) return false;
        if (labelEnd + 1 >= line.Length || line[labelEnd + 1] != ':') return false;

        string label = line.Substring(p + 2, labelEnd - p - 2).Trim();
        if (label.Length is 0 or > BitMarkdownLinkHelpers.MaxLabelLength) return false;

        string title = line[(labelEnd + 2)..].Trim();
        // A definition with nothing on the right explains nothing; it is ordinary text.
        if (title.Length == 0) return false;

        output.Add(new BitMarkdownAbbreviationDefinitionNode
        {
            Label = label,
            Title = BitMarkdownEntities.Decode(title)
        });
        state.Line++;
        return true;
    }
}
