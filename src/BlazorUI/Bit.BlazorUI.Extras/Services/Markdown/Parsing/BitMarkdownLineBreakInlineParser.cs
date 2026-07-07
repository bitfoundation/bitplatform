namespace Bit.BlazorUI;

/// <summary>Turns newlines into soft breaks, or hard breaks after two trailing spaces.</summary>
public sealed class BitMarkdownViewerLineBreakInlineParser : BitMarkdownViewerInlineParser
{
    public override char[] TriggerChars => new[] { '\n' };

    public override bool TryParse(BitMarkdownViewerInlineProcessor state)
    {
        int removed = state.TrimPendingTrailingSpaces();
        state.AppendNode(new BitMarkdownViewerLineBreakNode { Hard = removed >= 2 });
        state.Pos++;
        return true;
    }
}
