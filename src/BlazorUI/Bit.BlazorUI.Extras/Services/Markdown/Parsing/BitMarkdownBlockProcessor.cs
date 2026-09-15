namespace Bit.BlazorUI;

/// <summary>
/// Drives block-level parsing: iterates source lines and lets the pipeline's
/// <see cref="BitMarkdownBlockParser"/>s (in priority order) consume them. Provides the shared
/// helpers and recursion entry points block parsers rely on.
/// </summary>
public sealed class BitMarkdownBlockProcessor
{
    internal BitMarkdownBlockProcessor(BitMarkdownPipeline pipeline, IReadOnlyList<string> lines)
        : this(pipeline, lines, BitMarkdownParseContext.Empty, 0, UnknownLine)
    {
    }

    internal BitMarkdownBlockProcessor(BitMarkdownPipeline pipeline, IReadOnlyList<string> lines, BitMarkdownParseContext context, int depth, int lineOffset)
    {
        Pipeline = pipeline;
        Lines = lines;
        Context = context;
        Depth = depth;
        LineOffset = lineOffset;
    }

    /// <summary>The value a source line takes when it cannot be traced back to the document.</summary>
    internal const int UnknownLine = -1;

    public BitMarkdownPipeline Pipeline { get; }

    /// <summary>The state shared by every parser taking part in this parse.</summary>
    internal BitMarkdownParseContext Context { get; }

    /// <summary>The safety limits in effect for this parse.</summary>
    internal BitMarkdownParseOptions Options => Context.Options;

    /// <summary>
    /// True when the document declares a <c>[label]:</c> definition line for the supplied
    /// (already normalized) label. Block and inline parsers consult this before treating
    /// bracketed text as a reference, since definitions may appear after their references.
    /// </summary>
    public bool HasReferenceLabel(string normalizedLabel) => Context.ReferenceLabels.Contains(normalizedLabel);

    /// <summary>The current nesting depth of this processor within the document.</summary>
    internal int Depth { get; }

    /// <summary>
    /// True when these lines are the document's own, rather than the inside of a block quote, a
    /// list item or any other nested container. A construct that is only meaningful at the top of
    /// a file - front matter, for one - checks this before claiming its lines.
    /// </summary>
    public bool IsTopLevel => Depth == 0;

    /// <summary>The lines being parsed in the current scope.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>Index of the line currently being considered.</summary>
    public int Line { get; set; }

    /// <summary>
    /// The document line <c>Lines[0]</c> was taken from, or <see cref="UnknownLine"/> when these
    /// lines cannot be traced back to it. Every container that recurses hands its inner lines the
    /// document line the first of them came from, so a node parsed inside a list item inside a
    /// block quote still knows which line of the source it was written on.
    /// </summary>
    internal int LineOffset { get; }

    /// <summary>
    /// The document line <paramref name="localLine"/> of this scope was written on, or
    /// <see cref="UnknownLine"/> when it cannot be traced back to the source. This is what lets a
    /// node be edited in the source it came from - a ticked task box being written back into its
    /// own <c>[ ]</c> marker - without a second scanner that has to agree with this parser.
    /// </summary>
    public int SourceLine(int localLine)
        => LineOffset == UnknownLine ? UnknownLine : LineOffset + localLine;

    internal List<BitMarkdownNode> Run()
    {
        var output = new List<BitMarkdownNode>();
        while (Line < Lines.Count)
        {
            if (IsBlank(Lines[Line])) { Line++; continue; }

            bool matched = false;
            foreach (var parser in Pipeline.BlockParsers)
            {
                if (parser.TryParse(this, output))
                {
                    matched = true;
                    break;
                }
            }
            if (!matched) Line++; // safety net; BitMarkdownParagraphParser is the catch-all
        }
        return output;
    }

    /// <summary>
    /// Recursively parses a nested set of lines (list items, block quotes) that cannot be traced
    /// back to the document's own lines.
    /// </summary>
    public List<BitMarkdownNode> ParseBlocks(IReadOnlyList<string> lines)
        => Pipeline.ParseBlocks(lines, Context, Depth + 1, UnknownLine);

    /// <summary>
    /// Recursively parses a nested set of lines, the first of which was taken from
    /// <paramref name="firstLine"/> of this scope. Pass the local index the inner lines start at -
    /// they have to map one-to-one onto the lines from there - so the nested nodes keep knowing
    /// which document line they were written on.
    /// </summary>
    public List<BitMarkdownNode> ParseBlocks(IReadOnlyList<string> lines, int firstLine)
        => Pipeline.ParseBlocks(lines, Context, Depth + 1, SourceLine(firstLine));

    /// <summary>Parses inline content using the pipeline's inline parsers.</summary>
    public List<BitMarkdownNode> ParseInlines(string text) => Pipeline.ParseInlines(text, Context, Depth + 1);

    /// <summary>True if any block parser (other than the paragraph fallback) starts at the line.</summary>
    public bool StartsBlock(int lineIndex)
    {
        foreach (var parser in Pipeline.BlockParsers)
            if (parser.CanInterruptParagraph(this, lineIndex))
                return true;
        return false;
    }

    // -- shared helpers -----------------------------------------------------

    // A line is blank only when it is empty or made up solely of spaces and tabs.
    // Other Unicode whitespace (e.g. NBSP) must stay visible to the block parsers,
    // so a generic Trim() (which strips all whitespace) is intentionally avoided.
    public static bool IsBlank(string line)
    {
        foreach (char c in line)
            if (c != ' ' && c != '\t') return false;
        return true;
    }

    public static int GetIndent(string line)
    {
        int indent = 0;
        foreach (char c in line)
        {
            if (c == ' ') indent++;
            else if (c == '\t') indent += 4 - (indent % 4); // advance to next tab stop
            else break;
        }
        return indent;
    }

    public static string StripIndent(string line, int count)
    {
        int removed = 0, idx = 0;
        while (idx < line.Length && removed < count)
        {
            if (line[idx] == ' ') { removed++; idx++; }
            else if (line[idx] == '\t')
            {
                int width = 4 - (removed % 4); // visual width of this tab
                removed += width;
                idx++;
                if (removed > count)
                {
                    // The tab's width overshoots the requested column count; preserve
                    // the unused portion as leading spaces instead of dropping it.
                    return new string(' ', removed - count) + line[idx..];
                }
            }
            else break;
        }
        return line[idx..];
    }

    public static string TrimTrailingNewline(string s)
    {
        if (s.EndsWith("\r\n")) return s[..^2];
        if (s.EndsWith('\n')) return s[..^1];
        return s;
    }
}
