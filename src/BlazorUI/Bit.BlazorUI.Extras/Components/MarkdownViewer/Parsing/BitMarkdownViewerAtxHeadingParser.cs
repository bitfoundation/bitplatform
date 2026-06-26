namespace Bit.BlazorUI;

/// <summary>Parses ATX headings (<c># ... ######</c>).</summary>
public sealed class BitMarkdownViewerAtxHeadingParser : BitMarkdownViewerBlockParser
{
    public override int Order => 20;

    public override bool CanInterruptParagraph(BitMarkdownViewerBlockProcessor state, int lineIndex)
        => BitMarkdownViewerBlockGrammar.AtxHeading().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        var m = BitMarkdownViewerBlockGrammar.AtxHeading().Match(state.Lines[state.Line]);
        if (!m.Success) return false;

        var heading = new BitMarkdownViewerHeadingNode { Level = m.Groups[1].Value.Length };
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
