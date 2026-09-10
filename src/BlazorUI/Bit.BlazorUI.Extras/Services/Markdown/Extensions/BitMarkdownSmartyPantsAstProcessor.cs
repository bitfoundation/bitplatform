using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Replaces the ASCII stand-ins authors type with the typographic characters they mean: straight
/// quotes become curly ones, <c>--</c> and <c>---</c> become en and em dashes, <c>...</c> becomes
/// an ellipsis, and <c>&lt;&lt;</c> / <c>&gt;&gt;</c> become guillemets.
/// </summary>
/// <remarks>
/// Only <see cref="BitMarkdownTextNode"/>s are rewritten, so code spans, code blocks and URLs -
/// none of which are text nodes - keep every character exactly as it was written. Whether a quote
/// opens or closes is decided from the character before it, the same way a typesetter would: a
/// quote after whitespace or an opening bracket opens, and every other one closes. That also gives
/// <c>don't</c> its apostrophe.
/// <para>
/// That character is tracked across the whole block rather than per text node, because emphasis
/// splits a sentence into several of them: in <c>"**bold**"</c> the closing quote is a text node of
/// its own, and reading it in isolation would open a second quote instead of closing the first.
/// </para>
/// </remarks>
public sealed class BitMarkdownSmartyPantsAstProcessor : BitMarkdownAstProcessor
{
    // After the flavors that read the raw text (task markers, autolink literals, abbreviations):
    // rewriting an ellipsis or a quote inside a URL run before it was linked would change the
    // destination.
    public override int Order => 200;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        // Descendants walks the tree in document order, which is what lets one running "previous
        // character" carry from one text node to the next.
        char previous = '\0';

        foreach (var node in BitMarkdownAstHelper.Descendants(document))
        {
            switch (node)
            {
                case BitMarkdownTextNode text:
                    text.Text = Educate(text.Text, previous);
                    if (text.Text.Length > 0) previous = text.Text[^1];
                    break;

                // Code is not educated, but it is still text on the line: a quote right after it
                // closes rather than opens.
                case BitMarkdownCodeSpanNode code:
                    if (code.Content.Length > 0) previous = code.Content[^1];
                    break;

                case BitMarkdownLineBreakNode:
                    previous = ' ';
                    break;

                // A new block starts a new line of prose, so nothing before it can decide how its
                // first quote leans.
                case BitMarkdownParagraphNode:
                case BitMarkdownHeadingNode:
                case BitMarkdownListItemNode:
                case BitMarkdownBlockquoteNode:
                case BitMarkdownCodeBlockNode:
                    previous = '\0';
                    break;
            }
        }
    }

    /// <summary>Applies the substitutions to one run of text.</summary>
    public static string Educate(string text) => Educate(text, '\0');

    /// <summary>
    /// Applies the substitutions to one run of text, continuing from
    /// <paramref name="previous"/> - the last character of the run before it, or <c>'\0'</c> at the
    /// start of a block.
    /// </summary>
    public static string Educate(string text, char previous)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.AsSpan().IndexOfAny(".-\"'<>") < 0) return text;

        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            switch (c)
            {
                case '.' when Run(text, i, '.') >= 3:
                    sb.Append('…');
                    i += 2;
                    continue;

                case '-' when Run(text, i, '-') >= 3:
                    sb.Append('—');
                    i += 2;
                    continue;

                case '-' when Run(text, i, '-') == 2:
                    sb.Append('–');
                    i += 1;
                    continue;

                case '<' when Run(text, i, '<') >= 2:
                    sb.Append('«');
                    i += 1;
                    continue;

                case '>' when Run(text, i, '>') >= 2:
                    sb.Append('»');
                    i += 1;
                    continue;

                case '"':
                    sb.Append(Opens(sb, previous) ? '“' : '”');
                    continue;

                case '\'':
                    sb.Append(Opens(sb, previous) ? '‘' : '’');
                    continue;

                default:
                    sb.Append(c);
                    continue;
            }
        }
        return sb.ToString();
    }

    private static int Run(string s, int start, char c)
    {
        int i = start;
        while (i < s.Length && s[i] == c) i++;
        return i - start;
    }

    // A quote opens when nothing readable precedes it - the start of a block, a space, or an
    // opening bracket or dash. Everything else closes, which is also what turns the apostrophe in
    // "don't" into the right glyph.
    private static bool Opens(StringBuilder written, char previous)
    {
        char prev = written.Length > 0 ? written[^1] : previous;
        if (prev == '\0') return true;

        return char.IsWhiteSpace(prev) || prev is '(' or '[' or '{' or '—' or '–' or '“' or '‘';
    }
}
