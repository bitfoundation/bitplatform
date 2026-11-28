using System.Net;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// A tree of the markup <see cref="BitRichTextEditorHtmlSanitizer"/> writes, for the block model to read HTML through.
/// </summary>
/// <remarks>
/// It never reads markup the sanitizer has not written first. The sanitizer models the browser's parser and writes a
/// well-formed tree back - every element closed explicitly, every attribute quoted, every value encoded - so reading its
/// output needs no parsing rules of its own: a start tag opens, an end tag closes the element it opened, a void tag
/// opens nothing.
/// </remarks>
internal sealed partial class BitBlockHtmlTree
{
    private static readonly HashSet<string> VoidTags = ["area", "br", "col", "embed", "hr", "img", "input", "source", "track", "wbr"];

    private BitBlockHtmlTree(string? tag, string? text)
    {
        Tag = tag;
        Text = text;
    }

    /// <summary>The element's name, or null for a text node (and for the root).</summary>
    public string? Tag { get; }

    /// <summary>A text node's text, decoded; null for an element.</summary>
    public string? Text { get; }

    public Dictionary<string, string> Attributes { get; } = new(StringComparer.Ordinal);

    public List<BitBlockHtmlTree> Children { get; } = [];

    public bool IsText => Text is not null;

    public string? GetAttribute(string name) => Attributes.TryGetValue(name, out var value) ? value : null;

    /// <summary>The text of the node and everything inside it, with a <c>br</c> read as a newline.</summary>
    public string InnerText()
    {
        if (Text is not null) return Text;
        if (Tag == "br") return "\n";

        return string.Concat(Children.Select(c => c.InnerText()));
    }

    /// <summary>
    /// Sanitizes <paramref name="html"/> against <paramref name="policy"/> and reads the result into a tree, whose root
    /// is a nameless element holding the top-level nodes.
    /// </summary>
    /// <remarks>
    /// <c>ins</c> and <c>del</c> are not among the tags the sanitizer writes, so they are read as <c>u</c> and <c>s</c>
    /// on the way in, which it does write; the block model reads <c>u</c> as its <c>ins</c> mark.
    /// </remarks>
    public static BitBlockHtmlTree Parse(string? html, BitRichTextEditorSanitizationPolicy? policy)
    {
        var root = new BitBlockHtmlTree(null, null);
        if (string.IsNullOrEmpty(html)) return root;

        var renamed = InsertedOrDeleted().Replace(html, m => $"<{m.Groups[1].Value}{(m.Groups[2].Value.Equals("ins", StringComparison.OrdinalIgnoreCase) ? "u" : "s")}");
        var sanitized = BitRichTextEditorHtmlSanitizer.Sanitize(renamed, policy);

        var open = new Stack<BitBlockHtmlTree>();
        open.Push(root);

        var s = sanitized;
        var i = 0;
        while (i < s.Length)
        {
            if (s[i] != '<')
            {
                var next = s.IndexOf('<', i);
                if (next < 0) next = s.Length;
                open.Peek().Children.Add(new BitBlockHtmlTree(null, WebUtility.HtmlDecode(s[i..next])));
                i = next;
                continue;
            }

            var close = s.IndexOf('>', i);
            if (close < 0) break;

            if (s[i + 1] == '/')
            {
                if (open.Count > 1) open.Pop();
                i = close + 1;
                continue;
            }

            var element = ReadStartTag(s, i + 1, close);
            open.Peek().Children.Add(element);
            if (VoidTags.Contains(element.Tag!) is false) open.Push(element);
            i = close + 1;
        }

        return root;
    }

    // A start tag as the sanitizer writes it: <name attr="value" ...>, where no value holds a quote or an angle bracket.
    private static BitBlockHtmlTree ReadStartTag(string s, int start, int end)
    {
        var nameEnd = start;
        while (nameEnd < end && s[nameEnd] != ' ') nameEnd++;

        var element = new BitBlockHtmlTree(s[start..nameEnd], null);

        var i = nameEnd;
        while (i < end)
        {
            while (i < end && s[i] == ' ') i++;
            var eq = s.IndexOf('=', i, end - i);
            if (eq < 0) break;

            var name = s[i..eq];
            var valueStart = eq + 2;
            var valueEnd = s.IndexOf('"', valueStart, end - valueStart);
            if (valueEnd < 0) break;

            element.Attributes[name] = WebUtility.HtmlDecode(s[valueStart..valueEnd]);
            i = valueEnd + 1;
        }

        return element;
    }

    [GeneratedRegex(@"<(/?)(ins|del)(?=[\s/>])", RegexOptions.IgnoreCase)]
    private static partial Regex InsertedOrDeleted();
}
