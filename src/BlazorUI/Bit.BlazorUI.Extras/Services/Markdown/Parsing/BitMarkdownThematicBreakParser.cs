namespace Bit.BlazorUI;

/// <summary>Parses thematic breaks / horizontal rules.</summary>
public sealed class BitMarkdownViewerThematicBreakParser : BitMarkdownViewerBlockParser
{
    public override int Order => 30;

    public override bool CanInterruptParagraph(BitMarkdownViewerBlockProcessor state, int lineIndex)
        => BitMarkdownViewerBlockGrammar.ThematicBreak().IsMatch(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        if (!BitMarkdownViewerBlockGrammar.ThematicBreak().IsMatch(state.Lines[state.Line])) return false;
        output.Add(new BitMarkdownViewerThematicBreakNode());
        state.Line++;
        return true;
    }
}
