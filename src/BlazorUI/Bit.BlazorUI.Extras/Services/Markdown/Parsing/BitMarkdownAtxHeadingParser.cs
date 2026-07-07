namespace Bit.BlazorUI;

/// <summary>Parses ATX headings (<c># ... ######</c>).</summary>
public sealed class BitMarkdownAtxHeadingParser : BitMarkdownBlockParser
{
    public override int Order => 20;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => BitMarkdownBlockGrammar.AtxHeading().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var m = BitMarkdownBlockGrammar.AtxHeading().Match(state.Lines[state.Line]);
        if (!m.Success) return false;

        var heading = new BitMarkdownHeadingNode { Level = m.Groups[1].Value.Length };
        string content = m.Groups[2].Success ? StripClosingHashes(m.Groups[2].Value).Trim() : string.Empty;
        if (content.Length > 0)
            heading.Inlines.AddRange(state.ParseInlines(content));
        output.Add(heading);
        state.Line++;
        return true;
    }

    // Removes an optional trailing run of '#'s, but only when it is preceded by
    // whitespace (or is the whole content). "foo ###" -> "foo"; "foo###" stays.
    private static string StripClosingHashes(string content)
    {
        int hashStart = content.Length;
        while (hashStart > 0 && content[hashStart - 1] == '#') hashStart--;
        if (hashStart < content.Length && (hashStart == 0 || char.IsWhiteSpace(content[hashStart - 1])))
            return content[..hashStart];
        return content;
    }
}
