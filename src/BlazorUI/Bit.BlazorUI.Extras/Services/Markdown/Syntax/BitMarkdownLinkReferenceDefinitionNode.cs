namespace Bit.BlazorUI;

/// <summary>
/// A link reference definition (<c>[label]: /url "title"</c>). It is not part of the
/// rendered document: <see cref="BitMarkdownLinkReferenceAstProcessor"/> collects every
/// definition, removes it from the tree, and uses it to resolve the matching
/// <see cref="BitMarkdownLinkReferenceNode"/>s wherever they appear.
/// </summary>
public sealed class BitMarkdownLinkReferenceDefinitionNode : BitMarkdownNode
{
    /// <summary>The label as written in the source, before normalization.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>The normalized label used to match references (see <see cref="BitMarkdownLinkHelpers.NormalizeLabel"/>).</summary>
    public string NormalizedLabel { get; init; } = string.Empty;

    /// <summary>The destination, sanitized as a link.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// The destination as written, decoded but not yet sanitized. A definition does not know
    /// whether the references resolving against it are links or images, and the two are allowed
    /// different schemes - an embedded <c>data:image/png</c> is a valid image source and never a
    /// valid link - so each reference sanitizes this for what it actually is.
    /// </summary>
    public string RawUrl { get; init; } = string.Empty;

    /// <summary>The optional title.</summary>
    public string? Title { get; init; }
}
