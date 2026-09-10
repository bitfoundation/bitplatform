namespace Bit.BlazorUI;

/// <summary>
/// Runs a caller-supplied rewrite over every link and image destination in the document - what a
/// README written for a repository needs before it can be shown anywhere else, since every
/// relative path in it points at the repository rather than at the site rendering it.
/// </summary>
/// <remarks>
/// The rewrite runs after the URL has already been sanitized, and its result is sanitized again
/// before it reaches the DOM, so a rewriter cannot - by accident or otherwise - reintroduce a
/// <c>javascript:</c> destination. Returning <c>null</c> or an empty string drops the destination
/// and leaves the link's text (or the image's alt) behind.
/// </remarks>
public sealed class BitMarkdownUrlRewriteAstProcessor : BitMarkdownAstProcessor
{
    private readonly Func<BitMarkdownUrlRewriteContext, string?> _rewrite;

    public BitMarkdownUrlRewriteAstProcessor(Func<BitMarkdownUrlRewriteContext, string?> rewrite)
    {
        ArgumentNullException.ThrowIfNull(rewrite);
        _rewrite = rewrite;
    }

    // Last: every flavor that creates links (autolink literals, footnotes, reference links) has
    // to have created them before the destinations are rewritten.
    public override int Order => 1000;

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        BitMarkdownAstHelper.VisitChildLists(document, list =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i])
                {
                    case BitMarkdownLinkNode link:
                        string linkUrl = Rewrite(link.Url, isImage: false);
                        if (linkUrl == link.Url) break;
                        // Url is init-only, so the node is replaced by an identical one.
                        var replacement = new BitMarkdownLinkNode { Url = linkUrl, Title = link.Title };
                        replacement.Children.AddRange(link.Children);
                        list[i] = replacement;
                        break;

                    case BitMarkdownImageNode image:
                        string imageUrl = Rewrite(image.Url, isImage: true);
                        if (imageUrl == image.Url) break;
                        list[i] = new BitMarkdownImageNode
                        {
                            Url = imageUrl,
                            Title = image.Title,
                            Alt = image.Alt
                        };
                        break;
                }
            }
        });
    }

    private string Rewrite(string url, bool isImage)
    {
        string? rewritten = _rewrite(new BitMarkdownUrlRewriteContext(url, isImage));
        if (rewritten is null || rewritten.Length == 0) return string.Empty;
        if (rewritten == url) return url;

        return BitMarkdownUrlSanitizer.Sanitize(rewritten, isImage);
    }
}
