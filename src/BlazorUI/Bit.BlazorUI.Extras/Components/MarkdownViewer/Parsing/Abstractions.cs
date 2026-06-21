using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI.Markdown.Parsing;

/// <summary>
/// Parses a block-level construct. Block parsers are tried in ascending
/// <see cref="Order"/>; the first one that matches the current line wins.
/// </summary>
public abstract class BlockParser
{
    /// <summary>Relative priority. Lower runs first. The paragraph fallback uses a high value.</summary>
    public virtual int Order => 100;

    /// <summary>
    /// Attempts to parse a block starting at <see cref="BlockProcessor.Line"/>.
    /// On success the parser appends node(s) via <paramref name="output"/>, advances
    /// <see cref="BlockProcessor.Line"/> past the consumed lines, and returns true.
    /// </summary>
    public abstract bool TryParse(BlockProcessor state, List<MarkdownNode> output);

    /// <summary>
    /// True if this parser's construct begins at <paramref name="lineIndex"/> and is
    /// allowed to interrupt an open paragraph. Used to know where a paragraph ends.
    /// </summary>
    public virtual bool CanInterruptParagraph(BlockProcessor state, int lineIndex) => false;
}

/// <summary>
/// Parses an inline construct triggered by one of <see cref="TriggerChars"/>.
/// </summary>
public abstract class InlineParser
{
    /// <summary>Characters at which this parser should be consulted.</summary>
    public abstract char[] TriggerChars { get; }

    /// <summary>
    /// Attempts to parse at <see cref="InlineProcessor.Pos"/>. On success the parser
    /// emits node(s) via the processor, advances <see cref="InlineProcessor.Pos"/>,
    /// and returns true. On failure it must leave the position unchanged.
    /// </summary>
    public abstract bool TryParse(InlineProcessor state);
}

/// <summary>
/// Resolves runs of a delimiter character (e.g. <c>*</c>, <c>_</c>, <c>~</c>) into
/// wrapping inline nodes using the standard delimiter-stack algorithm.
/// </summary>
public abstract class DelimiterProcessor
{
    /// <summary>Delimiter characters handled by this processor.</summary>
    public abstract char[] Characters { get; }

    /// <summary>Minimum run length that can participate in matching.</summary>
    public virtual int MinRunLength => 1;

    /// <summary>Computes whether a delimiter run can open and/or close emphasis.</summary>
    public abstract (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next);

    /// <summary>
    /// Attempts to build a node from a matched opener/closer pair.
    /// Returns the number of delimiter characters consumed from each side
    /// (0 means the pair does not match for these lengths).
    /// </summary>
    public abstract int TryCreate(
        char c, int openLength, int closeLength, List<MarkdownNode> children, out MarkdownNode? node);
}

/// <summary>
/// Post-processes the parsed AST. Used by flavors such as task lists, autolinks,
/// emoji and auto-identifiers that operate after the tree has been built.
/// </summary>
public abstract class AstProcessor
{
    /// <summary>Relative priority. Lower runs first.</summary>
    public virtual int Order => 100;

    public abstract void Process(DocumentNode document, BitMarkdownPipeline pipeline);
}
