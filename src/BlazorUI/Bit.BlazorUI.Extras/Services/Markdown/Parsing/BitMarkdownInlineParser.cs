namespace Bit.BlazorUI;

/// <summary>
/// Parses an inline construct triggered by one of <see cref="TriggerChars"/>.
/// </summary>
public abstract class BitMarkdownInlineParser
{
    /// <summary>Characters at which this parser should be consulted.</summary>
    public abstract char[] TriggerChars { get; }

    /// <summary>
    /// Relative priority among the parsers sharing a trigger character. Lower is consulted
    /// first; the core parsers all use the default. An extension whose syntax is a special
    /// case of a core one (footnote references, for example, start like a link label) gives
    /// itself a lower value so it is offered the position first.
    /// </summary>
    public virtual int Order => 100;

    /// <summary>
    /// Attempts to parse at <see cref="BitMarkdownInlineProcessor.Pos"/>. On success the parser
    /// emits node(s) via the processor, advances <see cref="BitMarkdownInlineProcessor.Pos"/>,
    /// and returns true. On failure it must leave the position unchanged.
    /// </summary>
    public abstract bool TryParse(BitMarkdownInlineProcessor state);
}
