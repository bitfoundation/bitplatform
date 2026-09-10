namespace Bit.BlazorUI;

/// <summary>
/// Enables automatic heading <c>id</c> slugs, and optionally the permalink anchor that lets a
/// reader copy a link to a section.
/// </summary>
public sealed class BitMarkdownAutoIdentifierExtension : IBitMarkdownExtension
{
    /// <summary>Gives every heading an id, with no visible anchor.</summary>
    public BitMarkdownAutoIdentifierExtension() { }

    /// <summary>
    /// Gives every heading an id and, when <paramref name="anchorLinks"/> is <c>true</c>, appends a
    /// permalink to it.
    /// </summary>
    public BitMarkdownAutoIdentifierExtension(bool anchorLinks) => AnchorLinks = anchorLinks;

    /// <summary>True when each heading also gets a visible permalink.</summary>
    public bool AnchorLinks { get; }

    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new BitMarkdownAutoIdentifierAstProcessor(AnchorLinks));
        if (AnchorLinks)
        {
            builder.Renderers.Add(new BitMarkdownHeadingAnchorRenderer());
        }
    }
}
