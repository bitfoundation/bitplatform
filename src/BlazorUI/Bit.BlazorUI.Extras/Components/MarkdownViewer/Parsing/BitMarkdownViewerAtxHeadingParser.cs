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
        string content = m.Groups[2].Success ? m.Groups[2].Value.Trim() : string.Empty;
        if (content.Length > 0)
            heading.Inlines.AddRange(state.ParseInlines(content));
        output.Add(heading);
        state.Line++;
        return true;
    }
}
