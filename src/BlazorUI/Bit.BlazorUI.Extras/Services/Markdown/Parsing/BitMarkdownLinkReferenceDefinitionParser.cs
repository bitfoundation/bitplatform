using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Parses link reference definitions (<c>[label]: /url "optional title"</c>). A definition
/// produces no output of its own; it declares a label that reference links and images
/// elsewhere in the document resolve against.
/// </summary>
public sealed class BitMarkdownLinkReferenceDefinitionParser : BitMarkdownBlockParser
{
    // Definitions are recognized before every other construct except the fenced code and
    // footnote parsers, which claim their lines first.
    public override int Order => 5;

    public override bool TryParse(BitMarkdownBlockProcessor state, List<BitMarkdownNode> output)
    {
        var lines = state.Lines;
        int i = state.Line;

        // A run of consecutive definitions is consumed in one go, so "[a]: /a" followed by
        // "[b]: /b" does not leave the second one stranded as paragraph text.
        bool any = false;
        while (i < lines.Count && TryParseOne(lines, ref i, out var definition))
        {
            output.Add(definition!);
            any = true;
        }

        if (any is false) return false;

        state.Line = i;
        return true;
    }

    // A definition never interrupts an open paragraph (CommonMark); leaving
    // CanInterruptParagraph at its default false is what keeps that true.

    private static bool TryParseOne(IReadOnlyList<string> lines, ref int index, out BitMarkdownLinkReferenceDefinitionNode? definition)
    {
        definition = null;

        string line = lines[index];
        // Four spaces of indentation makes it an indented code block, not a definition.
        if (BitMarkdownBlockProcessor.GetIndent(line) >= 4) return false;

        int p = 0;
        while (p < line.Length && line[p] == ' ') p++;
        if (p >= line.Length || line[p] != '[') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(line, p);
        if (labelEnd < 0) return false;
        if (labelEnd + 1 >= line.Length || line[labelEnd + 1] != ':') return false;

        string label = line.Substring(p + 1, labelEnd - p - 1);
        // An all-whitespace label matches nothing and is not a definition.
        if (label.Length is 0 or > BitMarkdownLinkHelpers.MaxLabelLength) return false;
        string normalized = BitMarkdownLinkHelpers.NormalizeLabel(label);
        if (normalized.Length == 0) return false;

        int q = labelEnd + 2;
        while (q < line.Length && (line[q] is ' ' or '\t')) q++;
        if (TryReadDestination(line, ref q, out string url) is false) return false;

        int consumed = 1;
        string? title = null;

        int afterUrl = q;
        while (q < line.Length && (line[q] is ' ' or '\t')) q++;

        if (q < line.Length)
        {
            // A title on the definition's own line must be separated from the destination
            // and must be the last thing on that line.
            if (q == afterUrl) return false;
            if (TryReadTitle(line, ref q, out title) is false) return false;
            while (q < line.Length && (line[q] is ' ' or '\t')) q++;
            if (q < line.Length) return false;
        }
        else if (index + 1 < lines.Count)
        {
            // CommonMark allows the title to sit on the line following the destination.
            // If that line is not a valid, complete title the definition still stands and
            // the line is left for the next parser.
            string next = lines[index + 1];
            int t = 0;
            while (t < next.Length && (next[t] is ' ' or '\t')) t++;
            if (t < next.Length && TryReadTitle(next, ref t, out var nextTitle))
            {
                while (t < next.Length && (next[t] is ' ' or '\t')) t++;
                if (t == next.Length)
                {
                    title = nextTitle;
                    consumed = 2;
                }
            }
        }

        string decoded = BitMarkdownEntities.Decode(url);
        definition = new BitMarkdownLinkReferenceDefinitionNode
        {
            Label = label,
            NormalizedLabel = normalized,
            Url = BitMarkdownUrlSanitizer.Sanitize(decoded, isImage: false),
            RawUrl = decoded,
            Title = title
        };
        index += consumed;
        return true;
    }

    // Reads a link destination: either <bracketed> or a bare run with balanced parentheses.
    private static bool TryReadDestination(string line, ref int i, out string url)
    {
        url = string.Empty;
        if (i >= line.Length) return false;

        var sb = new StringBuilder();
        if (line[i] == '<')
        {
            i++;
            while (i < line.Length && line[i] != '>')
            {
                if (line[i] == '<') return false;
                if (line[i] == '\\' && i + 1 < line.Length) { sb.Append(line[i + 1]); i += 2; continue; }
                sb.Append(line[i++]);
            }
            if (i >= line.Length) return false;
            i++;
            url = sb.ToString();
            // "[a]: <>" is a valid definition pointing at an empty destination.
            return true;
        }

        int depth = 0;
        while (i < line.Length)
        {
            char c = line[i];
            if (c is ' ' or '\t') break;
            if (c == '\\' && i + 1 < line.Length) { sb.Append(line[i + 1]); i += 2; continue; }
            if (c == '(') depth++;
            else if (c == ')')
            {
                if (depth == 0) return false;
                depth--;
            }
            sb.Append(c);
            i++;
        }

        url = sb.ToString();
        return url.Length > 0;
    }

    // Reads a "title", 'title' or (title) that must be closed on the same line.
    private static bool TryReadTitle(string line, ref int i, out string? title)
    {
        title = null;
        if (i >= line.Length) return false;

        char open = line[i];
        if (open is not ('"' or '\'' or '(')) return false;
        char close = open == '(' ? ')' : open;

        var sb = new StringBuilder();
        int j = i + 1;
        while (j < line.Length && line[j] != close)
        {
            if (line[j] == '\\' && j + 1 < line.Length) { sb.Append(line[j + 1]); j += 2; continue; }
            sb.Append(line[j++]);
        }
        if (j >= line.Length) return false;

        title = BitMarkdownEntities.Decode(sb.ToString());
        i = j + 1;
        return true;
    }
}
