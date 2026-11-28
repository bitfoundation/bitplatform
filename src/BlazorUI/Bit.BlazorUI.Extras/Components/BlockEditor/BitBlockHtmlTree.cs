using System.Net;
using System.Text;

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
internal sealed class BitBlockHtmlTree
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

    /// <summary>Whether an <c>img</c> is anywhere inside the node.</summary>
    public bool HoldsImage() => Children.Any(c => c.Tag == "img" || c.HoldsImage());

    /// <summary>A copy of this element - its name and attributes - holding <paramref name="children"/>.</summary>
    public BitBlockHtmlTree WithChildren(IEnumerable<BitBlockHtmlTree> children)
    {
        var copy = new BitBlockHtmlTree(Tag, Text);
        foreach (var (name, value) in Attributes) copy.Attributes[name] = value;
        copy.Children.AddRange(children);
        return copy;
    }

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

        // Sanitized whole: a document read on purpose is never cut at the prerender's input limit.
        var sanitized = BitRichTextEditorHtmlSanitizer.Sanitize(RenameInsertedAndDeleted(html), policy, whole: true);

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

    // Renames the ins and del tags to u and s, as start and end tag names only: text, comments and attribute values
    // (quoted or not, since a tag is read up to its unquoted '>') are copied as they are.
    private static string RenameInsertedAndDeleted(string html)
    {
        if (html.Contains("ins", StringComparison.OrdinalIgnoreCase) is false
            && html.Contains("del", StringComparison.OrdinalIgnoreCase) is false) return html;

        var sb = new StringBuilder(html.Length);
        var i = 0;
        while (i < html.Length)
        {
            var lt = html.IndexOf('<', i);
            if (lt < 0)
            {
                sb.Append(html, i, html.Length - i);
                break;
            }

            sb.Append(html, i, lt - i);

            if (string.CompareOrdinal(html, lt, "<!--", 0, 4) == 0)
            {
                var close = html.IndexOf("-->", lt + 4, StringComparison.Ordinal);
                var end = close < 0 ? html.Length : close + 3;
                sb.Append(html, lt, end - lt);
                i = end;
                continue;
            }

            var nameStart = lt + 1 < html.Length && html[lt + 1] == '/' ? lt + 2 : lt + 1;
            if (nameStart >= html.Length || char.IsAsciiLetter(html[nameStart]) is false)
            {
                // Not a tag: a '<' of the text.
                sb.Append('<');
                i = lt + 1;
                continue;
            }

            var nameEnd = nameStart;
            while (nameEnd < html.Length && html[nameEnd] is not (' ' or '\t' or '\n' or '\r' or '\f' or '/' or '>')) nameEnd++;

            sb.Append(html, lt, nameStart - lt);
            var name = html.AsSpan(nameStart, nameEnd - nameStart);
            if (name.Equals("ins", StringComparison.OrdinalIgnoreCase)) sb.Append('u');
            else if (name.Equals("del", StringComparison.OrdinalIgnoreCase)) sb.Append('s');
            else sb.Append(name);

            // The rest of the tag, up to the '>' outside any quoted value (a quote opens one only after an '=').
            var j = nameEnd;
            var afterEquals = false;
            while (j < html.Length)
            {
                var c = html[j];
                if (afterEquals && c is '"' or '\'')
                {
                    var closeQuote = html.IndexOf(c, j + 1);
                    j = closeQuote < 0 ? html.Length : closeQuote + 1;
                    afterEquals = false;
                    continue;
                }

                j++;
                if (c == '>') break;
                if (c == '=') afterEquals = true;
                else if (c is not (' ' or '\t' or '\n' or '\r' or '\f')) afterEquals = false;
            }

            sb.Append(html, nameEnd, j - nameEnd);
            i = j;
        }

        return sb.ToString();
    }
}
