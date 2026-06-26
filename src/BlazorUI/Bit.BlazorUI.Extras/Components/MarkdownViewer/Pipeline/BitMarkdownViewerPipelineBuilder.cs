namespace Bit.BlazorUI;

/// <summary>
/// Configures a <see cref="BitMarkdownViewerPipeline"/>. A freshly created builder contains
/// only the basic CommonMark core; call <see cref="Use"/> (or the convenience
/// extension methods) to add flavors.
/// </summary>
public sealed class BitMarkdownViewerPipelineBuilder
{
    private readonly List<IBitMarkdownViewerExtension> _extensions = new();

    /// <summary>Block-level parsers. Sorted by <see cref="BitMarkdownViewerBlockParser.Order"/> at build time.</summary>
    public List<BitMarkdownViewerBlockParser> BlockParsers { get; } = new();

    /// <summary>Inline parsers consulted at their trigger characters.</summary>
    public List<BitMarkdownViewerInlineParser> InlineParsers { get; } = new();

    /// <summary>Delimiter processors for emphasis-like syntax.</summary>
    public List<BitMarkdownViewerDelimiterProcessor> DelimiterProcessors { get; } = new();

    /// <summary>AST post-processors, run after parsing. Sorted by order at build time.</summary>
    public List<BitMarkdownViewerAstProcessor> AstProcessors { get; } = new();

    /// <summary>Node renderers. Later registrations take precedence over earlier ones.</summary>
    public List<BitMarkdownViewerNodeRenderer> Renderers { get; } = new();

    /// <summary>Creates a builder pre-populated with the basic CommonMark core.</summary>
    public BitMarkdownViewerPipelineBuilder()
    {
        // Core block parsers.
        BlockParsers.Add(new BitMarkdownViewerFencedCodeBlockParser());
        BlockParsers.Add(new BitMarkdownViewerAtxHeadingParser());
        BlockParsers.Add(new BitMarkdownViewerThematicBreakParser());
        BlockParsers.Add(new BitMarkdownViewerBlockquoteParser());
        BlockParsers.Add(new BitMarkdownViewerIndentedCodeBlockParser());
        BlockParsers.Add(new BitMarkdownViewerListParser());
        BlockParsers.Add(new BitMarkdownViewerParagraphParser());

        // Core inline parsers.
        InlineParsers.Add(new BitMarkdownViewerEscapeInlineParser());
        InlineParsers.Add(new BitMarkdownViewerCodeSpanInlineParser());
        InlineParsers.Add(new BitMarkdownViewerAutolinkInlineParser());
        InlineParsers.Add(new BitMarkdownViewerLinkInlineParser());
        InlineParsers.Add(new BitMarkdownViewerLineBreakInlineParser());

        // Core emphasis.
        DelimiterProcessors.Add(new BitMarkdownViewerEmphasisDelimiterProcessor());

        // Core renderer (registered first so plugin renderers can override it).
        Renderers.Add(new BitMarkdownViewerCoreRenderer());
    }

    /// <summary>Adds an extension. The same extension type is only applied once.</summary>
    public BitMarkdownViewerPipelineBuilder Use(IBitMarkdownViewerExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        if (_extensions.Any(e => e.GetType() == extension.GetType()))
            return this;
        // Register before Setup so a self-referential registration inside Setup is
        // caught by the duplicate check above instead of recursing infinitely.
        _extensions.Add(extension);
        extension.Setup(this);
        return this;
    }

    /// <summary>Builds an immutable, reusable pipeline.</summary>
    public BitMarkdownViewerPipeline Build() => new(this);
}
