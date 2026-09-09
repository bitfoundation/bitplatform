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

    /// <summary>The (already sanitized) destination.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>The optional title.</summary>
    public string? Title { get; init; }
}
