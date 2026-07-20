namespace Bit.BlazorUI;

/// <summary>Turns newlines into soft breaks, or hard breaks after two trailing spaces.</summary>
public sealed class BitMarkdownLineBreakInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '\n' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        int removed = state.TrimPendingTrailingSpaces();
        state.AppendNode(new BitMarkdownLineBreakNode { Hard = removed >= 2 });
        state.Pos++;
        return true;
    }
}
