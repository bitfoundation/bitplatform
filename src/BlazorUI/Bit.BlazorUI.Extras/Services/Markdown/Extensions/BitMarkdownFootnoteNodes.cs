namespace Bit.BlazorUI;

/// <summary>
/// A footnote definition (<c>[^label]: text</c>). The definition is written wherever the
/// author likes; <see cref="BitMarkdownFootnoteAstProcessor"/> lifts every one of them into
/// a single <see cref="BitMarkdownFootnotesNode"/> at the end of the document.
/// </summary>
public sealed class BitMarkdownFootnoteDefinitionNode : BitMarkdownNode
{
    /// <summary>The normalized label the references use to find this definition.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The 1-based number printed for this footnote, assigned in order of first reference.</summary>
    public int Number { get; set; }

    /// <summary>How many references point at this footnote, which is how many back-links it gets.</summary>
    public int ReferenceCount { get; set; }

    /// <summary>The blocks making up the footnote's content.</summary>
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;
}

/// <summary>
/// A footnote reference (<c>[^label]</c>), rendered as a superscript link down to the
/// footnote's entry.
/// </summary>
public sealed class BitMarkdownFootnoteReferenceNode : BitMarkdownNode
{
    /// <summary>The normalized label this reference points at.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// The reference exactly as it was written, used to put the source text back when no
    /// definition matches.
    /// </summary>
    public string RawText { get; init; } = string.Empty;

    /// <summary>The footnote's number, assigned by the AST processor.</summary>
    public int Number { get; set; }

    /// <summary>Which occurrence of this label this is, so each back-link has a unique target.</summary>
    public int Occurrence { get; set; } = 1;
}

/// <summary>
/// The footnotes section appended to the end of a document that uses footnotes. It holds
/// the referenced <see cref="BitMarkdownFootnoteDefinitionNode"/>s in reference order.
/// </summary>
public sealed class BitMarkdownFootnotesNode : BitMarkdownNode
{
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;
}
