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
        // Snapshot every registration list so a failed Setup can be fully reverted
        // to its exact pre-Setup state. A misbehaving extension may insert, remove,
        // or reorder items before throwing, so a tail-truncating rollback is not
        // enough; we restore the full contents. This keeps the builder reusable.
        // _extensions is snapshotted from before this extension was added so any
        // nested extensions registered via Use(...) inside Setup are also rolled
        // back; otherwise a retry would short-circuit on a half-applied extension.
        var extensions = _extensions.ToList();
        // Register before Setup so a self-referential registration inside Setup is
        // caught by the duplicate check above instead of recursing infinitely.
        _extensions.Add(extension);

        var blockParsers = BlockParsers.ToList();
        var inlineParsers = InlineParsers.ToList();
        var delimiterProcessors = DelimiterProcessors.ToList();
        var astProcessors = AstProcessors.ToList();
        var renderers = Renderers.ToList();
        try
        {
            extension.Setup(this);
        }
        catch
        {
            // Setup failed: undo every mutation it made so the builder isn't left in
            // a partially-registered state and the extension can be retried.
            Restore(_extensions, extensions);
            Restore(BlockParsers, blockParsers);
            Restore(InlineParsers, inlineParsers);
            Restore(DelimiterProcessors, delimiterProcessors);
            Restore(AstProcessors, astProcessors);
            Restore(Renderers, renderers);
            throw;
        }
        return this;
    }

    private static void Restore<T>(List<T> list, List<T> snapshot)
    {
        list.Clear();
        list.AddRange(snapshot);
    }

    /// <summary>Builds an immutable, reusable pipeline.</summary>
    public BitMarkdownViewerPipeline Build() => new(this);
}
