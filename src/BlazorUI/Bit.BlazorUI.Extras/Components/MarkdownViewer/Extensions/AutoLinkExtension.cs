using System.Text.RegularExpressions;
using Bit.BlazorUI.Markdown.Parsing;
using Bit.BlazorUI.Markdown.Syntax;

namespace Bit.BlazorUI.Markdown.Extensions;

/// <summary>
/// Turns bare URLs, <c>www.</c> hosts and email addresses appearing in plain text
/// into links (GitHub autolink literals).
/// </summary>
public sealed partial class AutoLinkAstProcessor : AstProcessor
{
    [GeneratedRegex(
        @"(?<url>https?://[^\s<]+[^\s<.,:;""')\]\}])" +
        @"|(?<www>www\.[^\s<]+[^\s<.,:;""')\]\}])" +
        @"|(?<email>[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})",
        RegexOptions.IgnoreCase)]
    private static partial Regex LinkPattern();

    public override void Process(DocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var list in document.ChildLists)
            Walk(list);
    }

    private static void Walk(IList<MarkdownNode> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            switch (list[i])
            {
                case TextNode t:
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
                case LinkNode:
                case ImageNode:
                    break;

                default:
                    foreach (var childList in list[i].ChildLists)
                        Walk(childList);
                    break;
            }
        }
    }

    private static List<MarkdownNode>? Split(string text)
    {
        var matches = LinkPattern().Matches(text);
        if (matches.Count == 0) return null;

        var result = new List<MarkdownNode>();
        int last = 0;
        foreach (Match m in matches)
        {
            if (m.Index > last)
                result.Add(new TextNode(text[last..m.Index]));

            string matched = m.Value;
            string href = m.Groups["www"].Success ? "http://" + matched
                : m.Groups["email"].Success ? "mailto:" + matched
                : matched;

            var link = new LinkNode { Url = href };
            link.Children.Add(new TextNode(matched));
            result.Add(link);
            last = m.Index + m.Length;
        }
        if (last < text.Length)
            result.Add(new TextNode(text[last..]));

        return result;
    }
}

/// <summary>Enables GitHub autolink literals (bare URLs and emails become links).</summary>
public sealed class AutoLinkExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new AutoLinkAstProcessor());
}
