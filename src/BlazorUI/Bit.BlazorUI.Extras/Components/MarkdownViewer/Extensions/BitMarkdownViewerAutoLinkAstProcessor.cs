using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Turns bare URLs, <c>www.</c> hosts and email addresses appearing in plain text
/// into links (GitHub autolink literals).
/// </summary>
public sealed partial class BitMarkdownViewerAutoLinkAstProcessor : BitMarkdownViewerAstProcessor
{
    [GeneratedRegex(
        @"\b(?:" +
        @"(?<url>https?://[^\s<]+[^\s<.,:;""')\]\}])" +
        @"|(?<www>www\.[^\s<]+[^\s<.,:;""')\]\}])" +
        @"|(?<email>[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})" +
        @")",
        RegexOptions.IgnoreCase)]
    private static partial Regex LinkPattern();

    public override void Process(BitMarkdownViewerDocumentNode document, BitMarkdownViewerPipeline pipeline)
    {
        foreach (var list in document.ChildLists)
            Walk(list);
    }

    private static void Walk(IList<BitMarkdownViewerMarkdownNode> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
                case BitMarkdownViewerTextNode t:
                    var replacement = Split(t.Text);
                    if (replacement is not null)
                    {
                        list.RemoveAt(i);
                        foreach (var node in replacement)
                            list.Insert(i++, node);
                        i--;
                    }
                    break;

                // Never autolink inside existing links/images.
                case BitMarkdownViewerLinkNode:
                case BitMarkdownViewerImageNode:
                    break;

                default:
                    foreach (var childList in list[i].ChildLists)
                        Walk(childList);
                    break;
            }
        }
    }

    private static List<BitMarkdownViewerMarkdownNode>? Split(string text)
    {
        var matches = LinkPattern().Matches(text);
        if (matches.Count == 0) return null;

        var result = new List<BitMarkdownViewerMarkdownNode>();
        int last = 0;
        foreach (Match m in matches)
        {
            if (m.Index > last)
                result.Add(new BitMarkdownViewerTextNode(text[last..m.Index]));

            string matched = m.Value;
            string href = m.Groups["www"].Success ? "http://" + matched
                : m.Groups["email"].Success ? "mailto:" + matched
                : matched;

            // Route through the shared sanitizer so autolinks get the same URL safety
            // treatment as explicit links/images.
            var link = new BitMarkdownViewerLinkNode { Url = BitMarkdownViewerUrlSanitizer.Sanitize(href, isImage: false) };
            link.Children.Add(new BitMarkdownViewerTextNode(matched));
            result.Add(link);
            last = m.Index + m.Length;
        }
        if (last < text.Length)
            result.Add(new BitMarkdownViewerTextNode(text[last..]));

        return result;
    }
}
