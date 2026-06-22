using Bit.BlazorUI.Markdown.Parsing;
using Bit.BlazorUI.Markdown.Rendering;
using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI;

/// <summary>
/// An immutable, reusable Markdown processing configuration produced by a
/// <see cref="BitMarkdownPipelineBuilder"/>. Pipelines are thread-safe and should be
/// cached and shared.
/// </summary>
public sealed class BitMarkdownPipeline
{
    private static BitMarkdownPipeline? _basic;

    /// <summary>A pipeline with only the basic CommonMark core (no flavors).</summary>
    public static BitMarkdownPipeline Basic => _basic ??= new BitMarkdownPipelineBuilder().Build();

    internal BitMarkdownPipeline(BitMarkdownPipelineBuilder builder)
    {
        BlockParsers = builder.BlockParsers.OrderBy(p => p.Order).ToArray();
        AstProcessors = builder.AstProcessors.OrderBy(p => p.Order).ToArray();
        Renderers = builder.Renderers.ToArray();

        // Map trigger chars -> inline parsers (preserving registration order).
        var byChar = new Dictionary<char, List<InlineParser>>();
        foreach (var parser in builder.InlineParsers)
            foreach (var c in parser.TriggerChars)
                (byChar.TryGetValue(c, out var l) ? l : byChar[c] = new()).Add(parser);
        InlineParsersByChar = byChar.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<InlineParser>)kv.Value);

        // Map delimiter chars -> processor.
        var delimByChar = new Dictionary<char, DelimiterProcessor>();
        foreach (var dp in builder.DelimiterProcessors)
            foreach (var c in dp.Characters)
                delimByChar[c] = dp;
        DelimiterByChar = delimByChar;
        DelimiterChars = new HashSet<char>(delimByChar.Keys);
    }

    internal IReadOnlyList<BlockParser> BlockParsers { get; }
    internal IReadOnlyList<AstProcessor> AstProcessors { get; }
    internal IReadOnlyList<NodeRenderer> Renderers { get; }
    internal IReadOnlyDictionary<char, IReadOnlyList<InlineParser>> InlineParsersByChar { get; }
    internal IReadOnlyDictionary<char, DelimiterProcessor> DelimiterByChar { get; }
    internal IReadOnlySet<char> DelimiterChars { get; }

    /// <summary>Parses Markdown source into an AST, applying all AST processors.</summary>
    public DocumentNode Parse(string? markdown)
    {
        var document = new DocumentNode();
        if (string.IsNullOrEmpty(markdown))
            return document;

        document.Children.AddRange(ParseBlocks(SplitLines(markdown)));

        foreach (var processor in AstProcessors)
            processor.Process(document, this);

        return document;
    }

    internal List<MarkdownNode> ParseBlocks(IReadOnlyList<string> lines)
        => new BlockProcessor(this, lines).Run();

    internal List<MarkdownNode> ParseInlines(string text)
        => new InlineProcessor(this).Parse(text);

    /// <summary>Creates a renderer bound to this pipeline's node renderers.</summary>
    public MarkdownRenderer CreateRenderer() => new(Renderers);

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
