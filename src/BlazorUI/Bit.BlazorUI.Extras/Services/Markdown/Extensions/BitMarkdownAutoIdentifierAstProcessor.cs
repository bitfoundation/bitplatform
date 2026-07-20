using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Assigns a URL-friendly <c>id</c> (GitHub-style slug) to every heading, ensuring
/// uniqueness within the document so headings can be deep-linked.
/// </summary>
public sealed class BitMarkdownAutoIdentifierAstProcessor : BitMarkdownAstProcessor
{
    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        var used = new Dictionary<string, int>();
        foreach (var heading in BitMarkdownAstHelper.Descendants(document).OfType<BitMarkdownHeadingNode>())
        {
            string baseSlug = Slugify(BitMarkdownInlineHelpers.PlainText(heading.Inlines));
            if (baseSlug.Length == 0) baseSlug = "section";

            string slug = baseSlug;
            if (used.TryGetValue(baseSlug, out int count))
            {
                do
                {
                    slug = $"{baseSlug}-{++count}";
                }
                while (used.ContainsKey(slug));
                used[baseSlug] = count;
            }
            used[slug] = 0;
            heading.Id = slug;
        }
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
