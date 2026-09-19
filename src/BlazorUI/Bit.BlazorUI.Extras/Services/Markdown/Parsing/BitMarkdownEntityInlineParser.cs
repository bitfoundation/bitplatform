namespace Bit.BlazorUI;

/// <summary>
/// Turns HTML entity references (<c>&amp;copy;</c>) and numeric character references
/// (<c>&amp;#169;</c>, <c>&amp;#xA9;</c>) into the characters they name. An unrecognized
/// sequence is left exactly as written.
/// </summary>
public sealed class BitMarkdownEntityInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '&' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        if (BitMarkdownEntities.TryDecodeAt(state.Text, state.Pos, out var value, out int length) is false)
            return false;

        // The decoded text is appended to the pending literal run, so it is ordinary text
        // that Blazor escapes on the way out: "&lt;b&gt;" shows the characters "<b>" and
        // can never become markup.
        state.AppendText(value);
        state.Pos += length;
        return true;
    }
}
