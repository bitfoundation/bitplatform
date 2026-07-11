namespace Bit.BlazorUI;

/// <summary>
/// An immutable, reusable Markdown processing configuration produced by a
/// <see cref="BitMarkdownPipelineBuilder"/>. Pipelines are thread-safe and should be
/// cached and shared.
/// </summary>
public sealed class BitMarkdownPipeline
{
    internal BitMarkdownPipeline(BitMarkdownPipelineBuilder builder)
    {
        BlockParsers = builder.BlockParsers.OrderBy(p => p.Order).ToArray();
        AstProcessors = builder.AstProcessors.OrderBy(p => p.Order).ToArray();
        Renderers = builder.Renderers.ToArray();

        // Map trigger chars -> inline parsers (preserving registration order).
        var byChar = new Dictionary<char, List<BitMarkdownInlineParser>>();
        foreach (var parser in builder.InlineParsers)
            foreach (var c in parser.TriggerChars)
                (byChar.TryGetValue(c, out var l) ? l : byChar[c] = new()).Add(parser);
        InlineParsersByChar = byChar.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<BitMarkdownInlineParser>)kv.Value);

        // Map delimiter chars -> processor. A char can only belong to one processor;
        // failing loudly beats silently dropping the earlier registration.
        var delimByChar = new Dictionary<char, BitMarkdownDelimiterProcessor>();
        foreach (var dp in builder.DelimiterProcessors)
        {
            foreach (var c in dp.Characters)
            {
                if (delimByChar.TryGetValue(c, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Delimiter character '{c}' of {dp.GetType().Name} is already registered by {existing.GetType().Name}.");
                }

                delimByChar[c] = dp;
            }
        }
        DelimiterByChar = delimByChar;
        DelimiterChars = new HashSet<char>(delimByChar.Keys);
    }

    internal IReadOnlyList<BitMarkdownBlockParser> BlockParsers { get; }
    internal IReadOnlyList<BitMarkdownAstProcessor> AstProcessors { get; }
    internal IReadOnlyList<BitMarkdownNodeRenderer> Renderers { get; }
    internal IReadOnlyDictionary<char, IReadOnlyList<BitMarkdownInlineParser>> InlineParsersByChar { get; }
    internal IReadOnlyDictionary<char, BitMarkdownDelimiterProcessor> DelimiterByChar { get; }
    internal IReadOnlySet<char> DelimiterChars { get; }

    /// <summary>Parses Markdown source into an AST, applying all AST processors.</summary>
    public BitMarkdownDocumentNode Parse(string? markdown) => Parse(markdown, BitMarkdownParseOptions.Default);

    /// <summary>Parses Markdown source into an AST using the supplied safety limits.</summary>
    internal BitMarkdownDocumentNode Parse(string? markdown, BitMarkdownParseOptions options)
    {
        var document = new BitMarkdownDocumentNode();
        if (string.IsNullOrEmpty(markdown))
            return document;

        document.Children.AddRange(ParseBlocks(SplitLines(markdown), options, 0));

        foreach (var processor in AstProcessors)
            processor.Process(document, this);

        return document;
    }

    internal List<BitMarkdownNode> ParseBlocks(IReadOnlyList<string> lines, BitMarkdownParseOptions options, int depth)
    {
        // Depth guard: stop recursing into ever-deeper nested blocks and instead keep
        // the remaining lines as a single plain-text paragraph. This caps recursion so
        // hostile input (e.g. ">>>>...") cannot trigger a StackOverflowException.
        if (depth > options.MaxDepth)
        {
            var para = new BitMarkdownParagraphNode();
            para.Inlines.Add(new BitMarkdownTextNode(string.Join("\n", lines)));
            return new List<BitMarkdownNode> { para };
        }

        return new BitMarkdownBlockProcessor(this, lines, options, depth).Run();
    }

    internal List<BitMarkdownNode> ParseInlines(string text, BitMarkdownParseOptions options, int depth)
    {
        if (depth > options.MaxDepth)
            return new List<BitMarkdownNode> { new BitMarkdownTextNode(text) };

        return new BitMarkdownInlineProcessor(this, options, depth).Parse(text);
    }

    /// <summary>Creates a renderer bound to this pipeline's node renderers.</summary>
    public BitMarkdownRenderer CreateRenderer() => new(Renderers);

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
