namespace Bit.BlazorUI;

/// <summary>
/// Collects every abbreviation definition, removes it from the rendered tree, and wraps each whole
/// -word occurrence of the term in the text with a <see cref="BitMarkdownAbbreviationNode"/>.
/// </summary>
/// <remarks>
/// Only text is rewritten, so a term inside a code span, a code block or a URL is left exactly as
/// written - an abbreviation is a reading aid, not a search-and-replace. Longer terms are matched
/// first, so defining both <c>HTML</c> and <c>HTML5</c> expands the longer one where it appears.
/// </remarks>
public sealed class BitMarkdownAbbreviationAstProcessor : BitMarkdownAstProcessor
{
    // After the core reference resolver and the text merge (0, 1) - so the terms are matched against
    // the text the author actually wrote rather than the fragments the scanner split it into - and
    // after the flavors that read whole runs of text themselves. Autolink literals in particular
    // have to see a URL as one piece: splitting an abbreviation out of it first would leave only
    // part of the URL linked.
    public override int Order => 150;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        var definitions = new Dictionary<string, string>(StringComparer.Ordinal);

        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is BitMarkdownAbbreviationDefinitionNode definition)
                    definitions.TryAdd(definition.Label, definition.Title);
            }

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is BitMarkdownAbbreviationDefinitionNode) list.RemoveAt(i);
            }
        });

        if (definitions.Count == 0) return;

        // Longest first, so "HTML5" wins over "HTML" where both are defined.
        var terms = definitions.Keys.OrderByDescending(k => k.Length).ToArray();

        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is not BitMarkdownTextNode text) continue;

                var replacement = Split(text.Text, terms, definitions);
                if (replacement is null) continue;

                list.RemoveAt(i);
                foreach (var node in replacement) list.Insert(i++, node);
                i--;
            }
        });
    }

    // Splits one run of text around the abbreviations it holds, returning null when it holds none.
    private static List<BitMarkdownNode>? Split(string text, string[] terms, Dictionary<string, string> definitions)
    {
        List<BitMarkdownNode>? result = null;
        int last = 0;
        int i = 0;

        while (i < text.Length)
        {
            string? match = null;
            foreach (var term in terms)
            {
                if (i + term.Length > text.Length) continue;
                if (string.CompareOrdinal(text, i, term, 0, term.Length) != 0) continue;
                if (IsWholeWord(text, i, term.Length) is false) continue;
                match = term;
                break;
            }

            if (match is null)
            {
                i++;
                continue;
            }

            result ??= [];
            if (i > last) result.Add(new BitMarkdownTextNode(text[last..i]));
            result.Add(new BitMarkdownAbbreviationNode { Text = match, Title = definitions[match] });
            i += match.Length;
            last = i;
        }

        if (result is null) return null;

        if (last < text.Length) result.Add(new BitMarkdownTextNode(text[last..]));
        return result;
    }

    // A term only counts where it stands on its own: "HTML" in "HTMLElement" is not the
    // abbreviation, and expanding it there would put an <abbr> around half a word.
    private static bool IsWholeWord(string text, int start, int length)
    {
        if (start > 0 && IsWordCharacter(text[start - 1])) return false;

        int end = start + length;
        return end >= text.Length || IsWordCharacter(text[end]) is false;
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
