using Bit.BlazorUI.Markdown.Parsing;
using Bit.BlazorUI.Markdown.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Configures a <see cref="BitMarkdownPipeline"/>. A freshly created builder contains
/// only the basic CommonMark core; call <see cref="Use"/> (or the convenience
/// extension methods) to add flavors.
/// </summary>
public sealed class BitMarkdownPipelineBuilder
{
    private readonly List<IBitMarkdownExtension> _extensions = new();

    /// <summary>Block-level parsers. Sorted by <see cref="BlockParser.Order"/> at build time.</summary>
    public List<BlockParser> BlockParsers { get; } = new();

    /// <summary>Inline parsers consulted at their trigger characters.</summary>
    public List<InlineParser> InlineParsers { get; } = new();

    /// <summary>Delimiter processors for emphasis-like syntax.</summary>
    public List<DelimiterProcessor> DelimiterProcessors { get; } = new();

    /// <summary>AST post-processors, run after parsing. Sorted by order at build time.</summary>
    public List<AstProcessor> AstProcessors { get; } = new();

    /// <summary>Node renderers. Later registrations take precedence over earlier ones.</summary>
    public List<NodeRenderer> Renderers { get; } = new();

    /// <summary>Creates a builder pre-populated with the basic CommonMark core.</summary>
    public BitMarkdownPipelineBuilder()
    {
        // Core block parsers.
        BlockParsers.Add(new FencedCodeBlockParser());
        BlockParsers.Add(new AtxHeadingParser());
        BlockParsers.Add(new ThematicBreakParser());
        BlockParsers.Add(new BlockquoteParser());
        BlockParsers.Add(new IndentedCodeBlockParser());
        BlockParsers.Add(new ListParser());
        BlockParsers.Add(new ParagraphParser());

        // Core inline parsers.
        InlineParsers.Add(new EscapeInlineParser());
        InlineParsers.Add(new CodeSpanInlineParser());
        InlineParsers.Add(new AutolinkInlineParser());
        InlineParsers.Add(new LinkInlineParser());
        InlineParsers.Add(new LineBreakInlineParser());

        // Core emphasis.
        DelimiterProcessors.Add(new EmphasisDelimiterProcessor());

        // Core renderer (registered first so plugin renderers can override it).
        Renderers.Add(new CoreRenderer());
    }

    /// <summary>Adds an extension. The same extension type is only applied once.</summary>
    public BitMarkdownPipelineBuilder Use(IBitMarkdownExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        if (_extensions.Any(e => e.GetType() == extension.GetType()))
            return this;
        extension.Setup(this);
        _extensions.Add(extension);
        return this;
    }

    /// <summary>Builds an immutable, reusable pipeline.</summary>
    public BitMarkdownPipeline Build() => new(this);
}
