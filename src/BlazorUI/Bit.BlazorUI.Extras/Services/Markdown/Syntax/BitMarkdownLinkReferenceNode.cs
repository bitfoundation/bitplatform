namespace Bit.BlazorUI;

/// <summary>
/// A reference link or image (<c>[text][ref]</c>, <c>[text][]</c>, <c>[text]</c>,
/// <c>![alt][ref]</c>) whose destination is not known yet, because a link reference
/// definition is allowed to appear after the reference that uses it.
/// <see cref="BitMarkdownLinkReferenceAstProcessor"/> replaces every one of these once the
/// whole document has been parsed - with a real link/image when the label resolves, and
/// with the literal source text (<see cref="RawPrefix"/> + label + <see cref="RawSuffix"/>)
/// when it does not.
/// </summary>
public sealed class BitMarkdownLinkReferenceNode : BitMarkdownNode
{
    /// <summary>The normalized label this reference points at.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>True for <c>![alt][ref]</c>.</summary>
    public bool IsImage { get; init; }

    /// <summary>The literal source text before the label (<c>[</c> or <c>![</c>).</summary>
    public string RawPrefix { get; init; } = "[";

    /// <summary>The literal source text after the label (<c>]</c>, <c>][]</c> or <c>][ref]</c>).</summary>
    public string RawSuffix { get; init; } = "]";

    /// <summary>The parsed link label / image alt content.</summary>
    public List<BitMarkdownNode> Children { get; } = new();

    public override IList<BitMarkdownNode> ChildNodes => Children;
}
