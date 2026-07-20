namespace Bit.BlazorUI;

/// <summary>
/// Parses an inline construct triggered by one of <see cref="TriggerChars"/>.
/// </summary>
public abstract class BitMarkdownInlineParser
{
    /// <summary>Characters at which this parser should be consulted.</summary>
    public abstract char[] TriggerChars { get; }

    /// <summary>
    /// Attempts to parse at <see cref="BitMarkdownInlineProcessor.Pos"/>. On success the parser
    /// emits node(s) via the processor, advances <see cref="BitMarkdownInlineProcessor.Pos"/>,
    /// and returns true. On failure it must leave the position unchanged.
    /// </summary>
    public abstract bool TryParse(BitMarkdownInlineProcessor state);
}
