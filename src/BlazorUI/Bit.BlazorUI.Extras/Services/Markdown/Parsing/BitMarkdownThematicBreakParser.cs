namespace Bit.BlazorUI;

/// <summary>Parses thematic breaks / horizontal rules.</summary>
public sealed class BitMarkdownThematicBreakParser : BitMarkdownBlockParser
{
    public override int Order => 30;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => BitMarkdownBlockGrammar.ThematicBreak().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        if (!BitMarkdownBlockGrammar.ThematicBreak().IsMatch(state.Lines[state.Line])) return false;
        output.Add(new BitMarkdownThematicBreakNode());
        state.Line++;
        return true;
    }
}
