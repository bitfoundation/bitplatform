namespace Bit.BlazorUI;

/// <summary>
/// Parses definition lists in the shape Pandoc and markdown-it use: a term on its own line,
/// followed by one or more definitions each opened by a <c>:</c> marker.
/// </summary>
/// <remarks>
/// <code>
/// Blazor
/// : A framework for building web UI with C#.
/// : Ships in .NET.
/// </code>
/// A definition's continuation lines are indented under its text, the way a list item's are, so a
/// definition can hold several paragraphs or a nested list.
/// </remarks>
public sealed class BitMarkdownDefinitionListBlockParser : BitMarkdownBlockParser
{
    // After lists (60) and before pipe tables (65): a term line is otherwise paragraph text, and
    // its ":" marker must not be confused with a table's alignment row.
    public override int Order => 63;

    public override bool CanInterruptParagraph(BitMarkdownBlockProcessor state, int lineIndex)
        => IsTerm(state.Lines, lineIndex);

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        if (IsTerm(lines, state.Line) is false) return false;

        var list = new BitMarkdownDefinitionListNode();
        int i = state.Line;

        while (i < lines.Count && IsTerm(lines, i))
        {
            var term = new BitMarkdownDefinitionTermNode();
            term.Inlines.AddRange(state.ParseInlines(lines[i].Trim()));
            list.Children.Add(term);
            i++;

            while (i < lines.Count && TryReadMarker(lines[i], out int contentColumn, out string first))
            {
                var content = new List<string> { first };
                i++;

                // Continuation lines belong to this definition while they are indented to (or past)
                // its text column, or are blank and followed by such a line.
                while (i < lines.Count)
                {
                    if (BitMarkdownBlockProcessor.IsBlank(lines[i]))
                    {
                        int j = i + 1;
                        while (j < lines.Count && BitMarkdownBlockProcessor.IsBlank(lines[j])) j++;
                        if (j < lines.Count && BitMarkdownBlockProcessor.GetIndent(lines[j]) >= contentColumn)
                        {
                            content.Add(string.Empty);
                            i++;
                            continue;
                        }
                        break;
                    }

                    if (BitMarkdownBlockProcessor.GetIndent(lines[i]) < contentColumn) break;

                    content.Add(BitMarkdownBlockProcessor.StripIndent(lines[i], contentColumn));
                    i++;
                }

                var description = new BitMarkdownDefinitionDescriptionNode();
                description.Children.AddRange(state.ParseBlocks(content));
                list.Children.Add(description);
            }

            // A blank line between one definition and the next term keeps the same list going.
            int next = i;
            while (next < lines.Count && BitMarkdownBlockProcessor.IsBlank(lines[next])) next++;
            if (next > i && next < lines.Count && IsTerm(lines, next)) i = next;
        }

        output.Add(list);
        state.Line = i;
        return true;
    }

    // A term is a non-blank, unindented line that is not itself a definition marker and whose next
    // line opens one. Requiring the marker on the following line is what keeps ordinary prose -
    // which never has one - out of definition lists entirely.
    private static bool IsTerm(IReadOnlyList<string> lines, int index)
    {
        if (index + 1 >= lines.Count) return false;

        string line = lines[index];
        if (BitMarkdownBlockProcessor.IsBlank(line)) return false;
        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;
        if (TryReadMarker(line, out _, out _)) return false;

        return TryReadMarker(lines[index + 1], out _, out _);
    }

    // Reads a ": definition" marker, returning the column its text starts at (so the definition's
    // continuation lines can be measured against it) and that first line of text.
    private static bool TryReadMarker(string line, out int contentColumn, out string first)
    {
        contentColumn = 0;
        first = string.Empty;

        int indent = 0;
        int i = 0;
        while (i < line.Length && (line[i] is ' ' or '\t'))
        {
            indent += line[i] == '\t' ? 4 - (indent % 4) : 1;
            i++;
        }
        if (indent > 3 || i >= line.Length || line[i] != ':') return false;

        i++;
        int spaces = 0;
        while (i < line.Length && (line[i] is ' ' or '\t'))
        {
            spaces += line[i] == '\t' ? 4 - ((indent + 1 + spaces) % 4) : 1;
            i++;
        }
        // The marker has to be followed by whitespace, so neither a lone ":" nor a ":::" container
        // fence is read as one.
        if (spaces == 0) return false;

        contentColumn = indent + 1 + spaces;
        first = line[i..];
        return true;
    }
}
