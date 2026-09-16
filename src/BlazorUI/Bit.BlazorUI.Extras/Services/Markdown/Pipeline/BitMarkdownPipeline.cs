using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// An immutable, reusable Markdown processing configuration produced by a
/// <see cref="BitMarkdownPipelineBuilder"/>. Pipelines are thread-safe and should be
/// cached and shared.
/// </summary>
public sealed partial class BitMarkdownPipeline
{
    internal BitMarkdownPipeline(BitMarkdownPipelineBuilder builder)
    {
        BlockParsers = builder.BlockParsers.OrderBy(p => p.Order).ToArray();
        AstProcessors = builder.AstProcessors.OrderBy(p => p.Order).ToArray();
        Renderers = builder.Renderers.ToArray();

        // Map trigger chars -> inline parsers. Parsers are ordered by their Order so an
        // extension can be consulted before a core parser sharing the same trigger (the
        // footnote parser must see '[' before the link parser does); OrderBy is stable,
        // so equal orders keep registration order.
        var byChar = new Dictionary<char, List<BitMarkdownInlineParser>>();
        foreach (var parser in builder.InlineParsers.OrderBy(p => p.Order))
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

        Texts = builder.Texts ?? BitMarkdownTexts.Default;
        _renderer = new BitMarkdownRenderer(Renderers, Texts);
    }

    private readonly BitMarkdownRenderer _renderer;

    // Matches a line that opens a link reference definition, a footnote definition, or any
    // other "[label]:" construct. Only the label is captured: the pre-scan exists purely to
    // know which labels a reference could possibly resolve to, never to parse the definition.
    // The label body excludes unescaped brackets and is bounded by CommonMark's 999-char
    // ceiling, so the pattern cannot backtrack catastrophically.
    [GeneratedRegex(@"^ {0,3}\[(?<label>(?:[^\[\]\\\n]|\\.){1,999})\]:", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ReferenceDefinitionStart();

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

        var lines = SplitLines(markdown);
        var context = new BitMarkdownParseContext(options, ScanReferenceLabels(lines));

        document.Children.AddRange(ParseBlocks(lines, context, 0, 0));

        foreach (var processor in AstProcessors)
            processor.Process(document, this);

        return document;
    }

    // Collects the normalized label of every "[label]:" line in the source. Reference
    // definitions are allowed to appear after the references that use them, so the inline
    // parsers cannot know from the text alone whether "[foo]" is a reference or ordinary
    // prose; consulting this set first keeps documents that declare no definitions parsing
    // exactly as they did before reference links existed.
    private static HashSet<string> ScanReferenceLabels(IReadOnlyList<string> lines)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            // Cheap rejection first: the vast majority of lines never open a definition.
            if (line.Length < 4 || line.IndexOf('[') < 0 || line.IndexOf("]:", StringComparison.Ordinal) < 0)
                continue;

            try
            {
                var m = ReferenceDefinitionStart().Match(line);
                if (m.Success)
                    labels.Add(BitMarkdownLinkHelpers.NormalizeLabel(m.Groups["label"].Value));
            }
            catch (RegexMatchTimeoutException)
            {
                // Pathological line: skip it rather than hang. A missed label only means
                // the corresponding reference degrades to literal text.
            }
        }
        return labels;
    }

    internal List<BitMarkdownNode> ParseBlocks(IReadOnlyList<string> lines, BitMarkdownParseContext context, int depth, int lineOffset)
    {
        // Depth guard: stop recursing into ever-deeper nested blocks and instead keep
        // the remaining lines as a single plain-text paragraph. This caps recursion so
        // hostile input (e.g. ">>>>...") cannot trigger a StackOverflowException.
        if (depth > context.Options.MaxDepth)
        {
            var para = new BitMarkdownParagraphNode();
            para.Inlines.Add(new BitMarkdownTextNode(string.Join("\n", lines)));
            return new List<BitMarkdownNode> { para };
        }

        return new BitMarkdownBlockProcessor(this, lines, context, depth, lineOffset).Run();
    }

    internal List<BitMarkdownNode> ParseInlines(string text, BitMarkdownParseContext context, int depth)
    {
        if (depth > context.Options.MaxDepth)
            return new List<BitMarkdownNode> { new BitMarkdownTextNode(text) };

        return new BitMarkdownInlineProcessor(this, context, depth).Parse(text);
    }

    /// <summary>
    /// The words this pipeline's renderers write themselves - alert titles, footnote back-links,
    /// the accessible names of the regions and controls the markup adds.
    /// </summary>
    public BitMarkdownTexts Texts { get; }

    /// <summary>Creates a renderer bound to this pipeline's node renderers.</summary>
    public BitMarkdownRenderer CreateRenderer() => new(Renderers, Texts);

    /// <summary>
    /// The renderer this pipeline hands its own callers. A renderer holds nothing but the
    /// pipeline's (immutable) renderer list, so one instance serves every render of every
    /// component sharing the pipeline instead of being allocated per render.
    /// </summary>
    public BitMarkdownRenderer Renderer => _renderer;

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
