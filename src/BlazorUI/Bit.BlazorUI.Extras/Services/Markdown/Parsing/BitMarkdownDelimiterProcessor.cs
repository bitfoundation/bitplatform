namespace Bit.BlazorUI;

/// <summary>
/// Resolves runs of a delimiter character (e.g. <c>*</c>, <c>_</c>, <c>~</c>) into
/// wrapping inline nodes using the standard delimiter-stack algorithm.
/// </summary>
public abstract class BitMarkdownViewerDelimiterProcessor
{
    /// <summary>Delimiter characters handled by this processor.</summary>
    public abstract char[] Characters { get; }

    /// <summary>Minimum run length that can participate in matching.</summary>
    public virtual int MinRunLength => 1;

    /// <summary>
    /// Whether the CommonMark "rule of three" (a multiple-of-three length constraint on
    /// opener/closer pairs) applies to this processor. It is specific to emphasis
    /// (<c>*</c>/<c>_</c>); other delimiter syntaxes (e.g. <c>~~</c>) must leave it off so
    /// their pairs are validated solely by <see cref="TryCreate"/>.
    /// </summary>
    public virtual bool AppliesRuleOfThree => false;

    /// <summary>Computes whether a delimiter run can open and/or close emphasis.</summary>
    public abstract (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next);

    /// <summary>
    /// Attempts to build a node from a matched opener/closer pair.
    /// Returns the number of delimiter characters consumed from each side
    /// (0 means the pair does not match for these lengths).
    /// </summary>
    public abstract int TryCreate(
        char c, int openLength, int closeLength, List<BitMarkdownViewerMarkdownNode> children, out BitMarkdownViewerMarkdownNode? node);
}
