using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI.Markdown.Parsing;

/// <summary>
/// Drives block-level parsing: iterates source lines and lets the pipeline's
/// <see cref="BlockParser"/>s (in priority order) consume them. Provides the shared
/// helpers and recursion entry points block parsers rely on.
/// </summary>
public sealed class BlockProcessor
{
    internal BlockProcessor(BitMarkdownPipeline pipeline, IReadOnlyList<string> lines)
    {
        Pipeline = pipeline;
        Lines = lines;
    }

    public BitMarkdownPipeline Pipeline { get; }

    /// <summary>The lines being parsed in the current scope.</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>Index of the line currently being considered.</summary>
    public int Line { get; set; }

    internal List<MarkdownNode> Run()
    {
        var output = new List<MarkdownNode>();
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
            if (!matched) Line++; // safety net; ParagraphParser is the catch-all
        }
        return output;
    }

    /// <summary>Recursively parses a nested set of lines (list items, block quotes).</summary>
    public List<MarkdownNode> ParseBlocks(IReadOnlyList<string> lines) => Pipeline.ParseBlocks(lines);

    /// <summary>Parses inline content using the pipeline's inline parsers.</summary>
    public List<MarkdownNode> ParseInlines(string text) => Pipeline.ParseInlines(text);

    /// <summary>True if any block parser (other than the paragraph fallback) starts at the line.</summary>
    public bool StartsBlock(int lineIndex)
    {
        foreach (var parser in Pipeline.BlockParsers)
            if (parser.CanInterruptParagraph(this, lineIndex))
                return true;
        return false;
    }

    // -- shared helpers -----------------------------------------------------

    public static bool IsBlank(string line) => line.Trim().Length == 0;

    public static int GetIndent(string line)
    {
        int indent = 0;
        foreach (char c in line)
        {
            if (c == ' ') indent++;
            else if (c == '\t') indent += 4;
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
            else if (line[idx] == '\t') { removed += 4; idx++; }
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
