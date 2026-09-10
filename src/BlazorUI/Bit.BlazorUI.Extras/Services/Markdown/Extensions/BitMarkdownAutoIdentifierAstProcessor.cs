using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Assigns a URL-friendly <c>id</c> (GitHub-style slug) to every heading, ensuring
/// uniqueness within the document so headings can be deep-linked.
/// </summary>
/// <remarks>
/// A heading may also name its own id, by ending with <c>{#the-id}</c>. That is worth doing for a
/// heading whose slug would otherwise change with its wording, since every link already pointing
/// at it breaks the moment it does. The marker is removed from the rendered text.
/// </remarks>
public sealed class BitMarkdownAutoIdentifierAstProcessor : BitMarkdownAstProcessor
{
    /// <summary>Creates a processor that only assigns ids.</summary>
    public BitMarkdownAutoIdentifierAstProcessor() { }

    /// <summary>
    /// Creates a processor that also appends a <see cref="BitMarkdownHeadingAnchorNode"/> permalink
    /// to every heading when <paramref name="anchorLinks"/> is <c>true</c>.
    /// </summary>
    public BitMarkdownAutoIdentifierAstProcessor(bool anchorLinks) => AnchorLinks = anchorLinks;

    /// <summary>True when each heading also gets a visible permalink.</summary>
    public bool AnchorLinks { get; }

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        var used = new Dictionary<string, int>();
        foreach (var heading in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownHeadingNode>())
        {
            // An id already on the node is the author's - set by hand, or by a previous run over
            // this same tree - and is kept as it is. It is still claimed here, so a later heading
            // that slugs to the same text is suffixed instead of duplicating it.
            string? slug = string.IsNullOrEmpty(heading.Id) ? TakeExplicitId(heading) : heading.Id;

            if (string.IsNullOrEmpty(slug))
            {
                string baseSlug = Slugify(BitMarkdownInlineHelpers.PlainText(heading.Inlines));
                if (baseSlug.Length == 0) baseSlug = "section";

                slug = baseSlug;
                if (used.TryGetValue(baseSlug, out int count))
                {
                    do
                    {
                        slug = $"{baseSlug}-{++count}";
                    }
                    while (used.ContainsKey(slug));
                    used[baseSlug] = count;
                }
            }

            used.TryAdd(slug, 0);
            bool alreadyAnchored = heading.Id == slug && heading.Inlines.Count > 0
                                   && heading.Inlines[^1] is BitMarkdownHeadingAnchorNode;
            heading.Id = slug;

            if (AnchorLinks && alreadyAnchored is false)
            {
                // Appended after the slug is computed, so the anchor never feeds its own "#" back
                // into the heading text the slug is derived from.
                heading.Inlines.Add(new BitMarkdownHeadingAnchorNode
                {
                    Id = slug,
                    HeadingText = BitMarkdownInlineHelpers.PlainText(heading.Inlines)
                });
            }
        }
    }

    /// <summary>
    /// Reads and removes a trailing <c>{#the-id}</c> from the heading's text, returning the id it
    /// named or <c>null</c> when it carried none.
    /// </summary>
    private static string? TakeExplicitId(BitMarkdownHeadingNode heading)
    {
        if (heading.Inlines.Count == 0) return null;
        if (heading.Inlines[^1] is not BitMarkdownTextNode last) return null;

        string text = last.Text;
        int end = text.Length;
        while (end > 0 && char.IsWhiteSpace(text[end - 1])) end--;
        if (end < 4 || text[end - 1] != '}') return null;

        int open = text.LastIndexOf('{', end - 1);
        if (open < 0 || open + 2 >= end || text[open + 1] != '#') return null;

        string id = text[(open + 2)..(end - 1)].Trim();
        // An id has to be usable in a fragment; anything with whitespace or a brace in it was not
        // meant as one.
        if (id.Length == 0 || id.Any(c => char.IsWhiteSpace(c) || c is '{' or '}' or '#')) return null;

        last.Text = text[..open].TrimEnd();
        // A heading that was nothing but its id marker leaves an empty run behind.
        if (last.Text.Length == 0) heading.Inlines.RemoveAt(heading.Inlines.Count - 1);

        return id;
    }

    private static string Slugify(string text)
    {
        var sb = new StringBuilder(text.Length);
        bool lastDash = false;
        foreach (char c in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastDash = false;
            }
            else if (c is ' ' or '-' or '_')
            {
                if (!lastDash && sb.Length > 0)
                {
                    sb.Append('-');
                    lastDash = true;
                }
            }
            // other punctuation is dropped
        }
        if (lastDash && sb.Length > 0) sb.Length--;
        return sb.ToString();
    }
}
