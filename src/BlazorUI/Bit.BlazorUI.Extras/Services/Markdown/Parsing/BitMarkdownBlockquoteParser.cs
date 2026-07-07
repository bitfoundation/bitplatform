namespace Bit.BlazorUI;

/// <summary>Parses block quotes (<c>&gt; ...</c>) with lazy continuation.</summary>
public sealed class BitMarkdownViewerBlockquoteParser : BitMarkdownViewerBlockParser
{
    public override int Order => 40;

    public override bool CanInterruptParagraph(BitMarkdownViewerBlockProcessor state, int lineIndex)
        => IsQuote(state.Lines[lineIndex]);

    public override bool TryParse(BitMarkdownViewerBlockProcessor state, List<BitMarkdownViewerMarkdownNode> output)
    {
        var lines = state.Lines;
        if (!IsQuote(lines[state.Line])) return false;

        var inner = new List<string>();
        int i = state.Line;
        // Lazy (unmarked) continuation lines are only valid while the current quoted
        // content is still an open paragraph. Once a non-paragraph block (e.g. a
        // heading) is the last quoted line, a following unmarked line falls outside.
        bool lastWasParagraph = false;
        while (i < lines.Count)
        {
            string l = lines[i];
            if (IsQuote(l))
            {
                string stripped = StripMarker(l);
                inner.Add(stripped);
                lastWasParagraph = !BitMarkdownViewerBlockProcessor.IsBlank(stripped)
                                   && !StartsBlockLine(state, stripped);
                i++;
            }
            else if (lastWasParagraph && !BitMarkdownViewerBlockProcessor.IsBlank(l) && !state.StartsBlock(i))
            {
                // A lazy continuation line is itself paragraph text, so the paragraph
                // remains open for subsequent lazy lines.
                inner.Add(l);
                i++;
            }
            else break;
        }

        var quote = new BitMarkdownViewerBlockquoteNode();
        quote.Children.AddRange(state.ParseBlocks(inner));
        output.Add(quote);
        state.Line = i;
        return true;
    }

    // Determines whether a stripped inner line would begin a (non-paragraph) block,
    // used to decide if the blockquote's current content is still an open paragraph
    // that an unmarked line may lazily continue.
    private static bool StartsBlockLine(BitMarkdownViewerBlockProcessor state, string strippedLine)
    {
        var probe = new BitMarkdownViewerBlockProcessor(state.Pipeline, [strippedLine]);
        return probe.StartsBlock(0);
    }

    internal static bool IsQuote(string line)
    {
        // A blockquote marker may be preceded by at most 3 spaces; 4+ spaces
        // make it an indented code block instead.
        int spaces = 0;
        while (spaces < line.Length && line[spaces] == ' ') spaces++;
        return spaces <= 3 && spaces < line.Length && line[spaces] == '>';
    }

    private static string StripMarker(string line)
    {
        string t = line.TrimStart(' ')[1..];
        // A single space or tab immediately after the '>' marker is part of the
        // marker syntax and is stripped; the remaining inner text is preserved.
        if (t.Length > 0 && (t[0] == ' ' || t[0] == '\t')) t = t[1..];
        return t;
    }
}
