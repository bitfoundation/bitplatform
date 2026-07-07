namespace Bit.BlazorUI;

/// <summary>
/// Parses an inline construct triggered by one of <see cref="TriggerChars"/>.
/// </summary>
public abstract class BitMarkdownViewerInlineParser
{
    /// <summary>Characters at which this parser should be consulted.</summary>
    public abstract char[] TriggerChars { get; }

    /// <summary>
    /// Attempts to parse at <see cref="BitMarkdownViewerInlineProcessor.Pos"/>. On success the parser
    /// emits node(s) via the processor, advances <see cref="BitMarkdownViewerInlineProcessor.Pos"/>,
    /// and returns true. On failure it must leave the position unchanged.
    /// </summary>
    public abstract bool TryParse(BitMarkdownViewerInlineProcessor state);
}
