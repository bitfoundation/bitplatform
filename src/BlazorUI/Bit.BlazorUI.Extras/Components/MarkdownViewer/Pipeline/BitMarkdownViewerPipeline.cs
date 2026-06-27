namespace Bit.BlazorUI;

/// <summary>
/// An immutable, reusable Markdown processing configuration produced by a
/// <see cref="BitMarkdownViewerPipelineBuilder"/>. Pipelines are thread-safe and should be
/// cached and shared.
/// </summary>
public sealed class BitMarkdownViewerPipeline
{
    private static readonly Lazy<BitMarkdownViewerPipeline> _basic =
        new(() => new BitMarkdownViewerPipelineBuilder().Build());

    /// <summary>A pipeline with only the basic CommonMark core (no flavors).</summary>
    public static BitMarkdownViewerPipeline Basic => _basic.Value;

    internal BitMarkdownViewerPipeline(BitMarkdownViewerPipelineBuilder builder)
    {
        BlockParsers = builder.BlockParsers.OrderBy(p => p.Order).ToArray();
        AstProcessors = builder.AstProcessors.OrderBy(p => p.Order).ToArray();
        Renderers = builder.Renderers.ToArray();

        // Map trigger chars -> inline parsers (preserving registration order).
        var byChar = new Dictionary<char, List<BitMarkdownViewerInlineParser>>();
        foreach (var parser in builder.InlineParsers)
            foreach (var c in parser.TriggerChars)
                (byChar.TryGetValue(c, out var l) ? l : byChar[c] = new()).Add(parser);
        InlineParsersByChar = byChar.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<BitMarkdownViewerInlineParser>)kv.Value);

        // Map delimiter chars -> processor.
        var delimByChar = new Dictionary<char, BitMarkdownViewerDelimiterProcessor>();
        foreach (var dp in builder.DelimiterProcessors)
            foreach (var c in dp.Characters)
                delimByChar[c] = dp;
        DelimiterByChar = delimByChar;
        DelimiterChars = new HashSet<char>(delimByChar.Keys);
    }

    internal IReadOnlyList<BitMarkdownViewerBlockParser> BlockParsers { get; }
    internal IReadOnlyList<BitMarkdownViewerAstProcessor> AstProcessors { get; }
    internal IReadOnlyList<BitMarkdownViewerNodeRenderer> Renderers { get; }
    internal IReadOnlyDictionary<char, IReadOnlyList<BitMarkdownViewerInlineParser>> InlineParsersByChar { get; }
    internal IReadOnlyDictionary<char, BitMarkdownViewerDelimiterProcessor> DelimiterByChar { get; }
    internal IReadOnlySet<char> DelimiterChars { get; }

    /// <summary>Parses Markdown source into an AST, applying all AST processors.</summary>
    public BitMarkdownViewerDocumentNode Parse(string? markdown)
    {
        var document = new BitMarkdownViewerDocumentNode();
        if (string.IsNullOrEmpty(markdown))
            return document;

        document.Children.AddRange(ParseBlocks(SplitLines(markdown)));

        foreach (var processor in AstProcessors)
            processor.Process(document, this);

        return document;
    }

    internal List<BitMarkdownViewerMarkdownNode> ParseBlocks(IReadOnlyList<string> lines)
        => new BitMarkdownViewerBlockProcessor(this, lines).Run();

    internal List<BitMarkdownViewerMarkdownNode> ParseInlines(string text)
        => new BitMarkdownViewerInlineProcessor(this).Parse(text);

    /// <summary>Creates a renderer bound to this pipeline's node renderers.</summary>
    public BitMarkdownViewerMarkdownRenderer CreateRenderer() => new(Renderers);

    private static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '\n' || ch == '\r')
            {
                lines.Add(text.Substring(start, i - start));
                // Treat "\r\n" as a single line boundary.
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                start = i + 1;
            }
        }
        lines.Add(text[start..]);
        return lines;
    }
}
