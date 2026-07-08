using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Turns bare URLs, <c>www.</c> hosts and email addresses appearing in plain text
/// into links (GitHub autolink literals).
/// </summary>
public sealed partial class BitMarkdownAutoLinkAstProcessor : BitMarkdownAstProcessor
{
    [GeneratedRegex(
        @"\b(?:" +
        @"(?<url>https?://[^\s<]+)" +
        @"|(?<www>www\.[^\s<]+)" +
        @"|(?<email>[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,})" +
        @")",
        RegexOptions.IgnoreCase,
        matchTimeoutMilliseconds: 1000)]
    private static partial Regex LinkPattern();

    public override void Process(BitMarkdownDocumentNode document, BitMarkdownPipeline pipeline)
    {
        foreach (var list in document.ChildLists)
            Walk(list);
    }

    private static void Walk(IList<BitMarkdownNode> root)
    {
        // Iterative traversal over child lists to avoid stack overflow on deeply
        // nested input, mirroring BitMarkdownAstHelper's approach. A dedicated stack is
        // used instead of VisitChildLists because we must skip the subtrees of existing
        // link/image nodes (never autolink inside them). Sibling lists are processed
        // independently, so the LIFO order doesn't affect the result.
        var stack = new Stack<IList<BitMarkdownNode>>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var list = stack.Pop();
            for (int i = 0; i < list.Count; i++)
            {
                switch (list[i])
                {
                    case BitMarkdownTextNode t:
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
                    case BitMarkdownLinkNode:
                    case BitMarkdownImageNode:
                        break;

                    default:
                        foreach (var childList in list[i].ChildLists)
                            stack.Push(childList);
                        break;
                }
            }
        }
    }

    private static List<BitMarkdownNode>? Split(string text)
    {
        MatchCollection matches;
        try
        {
            matches = LinkPattern().Matches(text);
            _ = matches.Count; // Force evaluation so a ReDoS timeout surfaces here.
        }
        catch (RegexMatchTimeoutException)
        {
            // Pathological input: skip autolinking this run rather than hang.
            return null;
        }
        if (matches.Count == 0) return null;

        var result = new List<BitMarkdownNode>();
        int last = 0;
        foreach (Match m in matches)
        {
            if (m.Index > last)
                result.Add(new BitMarkdownTextNode(text[last..m.Index]));

            string matched = m.Value;
            // The url/www patterns greedily grab trailing punctuation; trim the part
            // that conventionally isn't a link character so the surplus stays as plain
            // text. (Emails are already tightly bounded by their own pattern.)
            if (!m.Groups["email"].Success)
                matched = matched[..TrimmedLength(matched)];

            string href = m.Groups["www"].Success ? "http://" + matched
                : m.Groups["email"].Success ? "mailto:" + matched
                : matched;

            // Route through the shared sanitizer so autolinks get the same URL safety
            // treatment as explicit links/images.
            var link = new BitMarkdownLinkNode { Url = BitMarkdownUrlSanitizer.Sanitize(href, isImage: false) };
            link.Children.Add(new BitMarkdownTextNode(matched));
            result.Add(link);
            // Advance only past the characters kept in the link so any trimmed
            // trailing punctuation is re-emitted as plain text.
            last = m.Index + matched.Length;
        }
        if (last < text.Length)
            result.Add(new BitMarkdownTextNode(text[last..]));

        return result;
    }

    // Returns the length of the URL candidate after trimming trailing punctuation
    // that is conventionally not part of a bare URL. Sentence punctuation is always
    // trimmed; closing brackets are trimmed only when unbalanced (more closers than
    // matching openers), so URLs like "https://e.com/Foo_(bar)" keep their bracket.
    private static int TrimmedLength(string url)
    {
        int end = url.Length;
        while (end > 0)
        {
            char c = url[end - 1];
            if (c is '.' or ',' or ':' or ';' or '!' or '?' or '"' or '\'')
            {
                end--;
                continue;
            }
            if (c is ')' or ']' or '}')
            {
                char open = c == ')' ? '(' : c == ']' ? '[' : '{';
                int closers = 0, openers = 0;
                for (int k = 0; k < end; k++)
                {
                    if (url[k] == c) closers++;
                    else if (url[k] == open) openers++;
                }
                if (closers > openers) { end--; continue; }
            }
            break;
        }
        return end;
    }
}
