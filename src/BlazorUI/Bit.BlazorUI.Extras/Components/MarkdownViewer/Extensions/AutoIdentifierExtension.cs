using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Assigns a URL-friendly <c>id</c> (GitHub-style slug) to every heading, ensuring
/// uniqueness within the document so headings can be deep-linked.
/// </summary>
public sealed class AutoIdentifierAstProcessor : AstProcessor
{
    public override void Process(DocumentNode document, BitMarkdownPipeline pipeline)
    {
        var used = new Dictionary<string, int>();
        foreach (var heading in AstHelper.Descendants(document).OfType<HeadingNode>())
        {
            string baseSlug = Slugify(InlineHelpers.PlainText(heading.Inlines));
            if (baseSlug.Length == 0) baseSlug = "section";

            string slug = baseSlug;
            if (used.TryGetValue(baseSlug, out int count))
            {
                used[baseSlug] = ++count;
                slug = $"{baseSlug}-{count}";
            }
            else
            {
                used[baseSlug] = 0;
            }
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

/// <summary>Enables automatic heading <c>id</c> slugs.</summary>
public sealed class AutoIdentifierExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new AutoIdentifierAstProcessor());
}
