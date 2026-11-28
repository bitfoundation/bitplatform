using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// The inline content of a text block: the vocabulary <see cref="BitBlock.Text"/> holds (<c>b i s code a sub sup mark ins
/// br</c>), read into a tree and written back in one canonical form.
/// </summary>
/// <remarks>
/// The canonical form is what makes a Markdown round trip exact: anything Markdown cannot say, or says only one way, is
/// settled here, so the writer and the reader meet on the same markup.
/// <list type="bullet">
/// <item>Any other tag is dropped and its text kept; a link keeps only an <c>href</c> the default sanitization policy
/// allows, and is dropped (its text kept) without one.</item>
/// <item>White space reads as HTML renders it: a run of spaces, tabs and newlines is one space, and none is kept at the
/// start or the end of the block or next to a line break.</item>
/// <item>A mark starts and ends on its text: white space and line breaks at its edges move out of it, and a mark left
/// empty goes. A mark inside the same mark, a link inside a link and any mark inside code are dropped, their text kept,
/// and two of the same mark side by side are one.</item>
/// <item>A line break at the very end of the block goes, as the browser never shows it.</item>
/// </list>
/// </remarks>
internal static class BitBlockInline
{
    // The tags the vocabulary keeps, by the name the sanitizer writes them under.
    private static readonly Dictionary<string, string> Marks = new(StringComparer.Ordinal)
    {
        ["b"] = "b", ["strong"] = "b",
        ["i"] = "i", ["em"] = "i",
        ["s"] = "s", ["strike"] = "s",
        ["u"] = "ins",
        ["code"] = "code",
        ["a"] = "a",
        ["sub"] = "sub",
        ["sup"] = "sup",
        ["mark"] = "mark",
        ["br"] = "br"
    };

    // The policy the text is sanitized with: the default one's link schemes, and only the tags the vocabulary keeps.
    private static readonly BitRichTextEditorSanitizationPolicy Policy = CreatePolicy();

    /// <summary>Reads inline HTML into the canonical tree.</summary>
    public static List<Node> Parse(string? html)
    {
        if (string.IsNullOrEmpty(html)) return [];

        var tree = BitBlockHtmlTree.Parse(html, Policy);
        return Canonical(FromTree(tree.Children));
    }

    /// <summary>Reads nodes of an HTML tree (a paragraph's, a list item's) into the canonical tree.</summary>
    public static List<Node> FromHtmlNodes(IEnumerable<BitBlockHtmlTree> nodes) => Canonical(FromTree(nodes));

    /// <summary>Puts nodes built by hand (by the Markdown reader) into the canonical form.</summary>
    public static List<Node> Canonical(List<Node> nodes)
    {
        nodes = Clean(nodes, insideLink: false, marks: []);
        CollapseSpaces(nodes);
        nodes = MoveEdgesOut(nodes);
        nodes = Merge(nodes);
        CollapseSpaces(nodes);
        TrimBlock(nodes);
        return Merge(nodes);
    }

    /// <summary>Returns <paramref name="html"/> in the canonical form.</summary>
    public static string Normalize(string? html) => Write(Parse(html));

    /// <summary>Writes the tree as the vocabulary's markup.</summary>
    public static string Write(IEnumerable<Node> nodes)
    {
        var sb = new StringBuilder();
        Write(sb, nodes, underlineTag: "ins");
        return sb.ToString();
    }

    /// <summary>
    /// Writes the tree as the HTML a RichTextEditor stores: the same markup, with the <c>ins</c> mark as <c>u</c>, which
    /// is the tag its underline writes.
    /// </summary>
    public static string WriteHtml(IEnumerable<Node> nodes)
    {
        var sb = new StringBuilder();
        Write(sb, nodes, underlineTag: "u");
        return sb.ToString();
    }

    /// <summary>The text of the tree, with a line break read as a newline.</summary>
    public static string PlainText(IEnumerable<Node> nodes)
    {
        var sb = new StringBuilder();
        foreach (var node in nodes) AppendPlainText(sb, node);
        return sb.ToString();
    }

    /// <summary>Escapes text for HTML content.</summary>
    public static string Encode(string text)
    {
        var sb = new StringBuilder(text.Length);
        AppendEncoded(sb, text, attribute: false);
        return sb.ToString();
    }

    /// <summary>Escapes text for a double-quoted HTML attribute.</summary>
    public static string EncodeAttribute(string text)
    {
        var sb = new StringBuilder(text.Length);
        AppendEncoded(sb, text, attribute: true);
        return sb.ToString();
    }

    private static BitRichTextEditorSanitizationPolicy CreatePolicy()
    {
        var defaults = BitRichTextEditorSanitizationPolicy.Default;

        return new()
        {
            AllowedTags = new HashSet<string>(Marks.Keys, StringComparer.OrdinalIgnoreCase),
            AllowedAttributes = new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" }
            },
            AllowedUriSchemes = defaults.AllowedUriSchemes,
            AllowDataImageUris = false,
            AllowedIframeHosts = []
        };
    }

    private static List<Node> FromTree(IEnumerable<BitBlockHtmlTree> nodes)
    {
        var result = new List<Node>();
        foreach (var node in nodes)
        {
            if (node.IsText)
            {
                result.Add(new TextNode(node.Text!));
                continue;
            }

            if (node.Tag is { } tag && Marks.TryGetValue(tag, out var mark))
            {
                result.Add(new ElementNode(mark, mark == "a" ? node.GetAttribute("href") : null, FromTree(node.Children)));
            }
            else
            {
                // Not in the vocabulary (a span, a paragraph a paste brought): its content stays, unwrapped.
                result.AddRange(FromTree(node.Children));
            }
        }

        return result;
    }

    // Drops what the vocabulary cannot nest: a link without a link address, a link in a link, a mark in the same mark,
    // anything in code. Newlines and tabs read as spaces, as HTML renders them.
    private static List<Node> Clean(List<Node> nodes, bool insideLink, HashSet<string> marks)
    {
        var result = new List<Node>();
        foreach (var node in nodes)
        {
            if (node is TextNode text)
            {
                result.Add(new TextNode(text.Text));
                continue;
            }

            var element = (ElementNode)node;
            if (element.Tag == "br")
            {
                result.Add(new ElementNode("br", null, []));
                continue;
            }

            if (element.Tag == "code")
            {
                if (marks.Contains("code"))
                {
                    result.Add(new TextNode(PlainText(element.Children).Replace('\n', ' ')));
                }
                else
                {
                    result.Add(new ElementNode("code", null, [new TextNode(PlainText(element.Children).Replace('\n', ' '))]));
                }
                continue;
            }

            var unwrap = marks.Contains(element.Tag)
                || (element.Tag == "a" && (insideLink || string.IsNullOrWhiteSpace(element.Href)));

            var inner = new HashSet<string>(marks) { element.Tag };
            var children = Clean(element.Children, insideLink || element.Tag == "a", unwrap ? marks : inner);

            if (unwrap)
            {
                result.AddRange(children);
            }
            else
            {
                // A space in a link address is written encoded, as Markdown's reader writes it, so either way in
                // reads the same address.
                result.Add(new ElementNode(element.Tag, element.Href?.Trim().Replace(" ", "%20"), children));
            }
        }

        return result;
    }

    // A run of HTML white space is one space, the way the browser renders it, within each text node.
    private static void CollapseSpaces(List<Node> nodes)
    {
        foreach (var node in nodes)
        {
            if (node is TextNode text)
            {
                text.Text = Collapse(text.Text);
            }
            else if (node is ElementNode element)
            {
                CollapseSpaces(element.Children);
            }
        }
    }

    private static string Collapse(string text)
    {
        var sb = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in text)
        {
            if (c is ' ' or '\t' or '\n' or '\r' or '\f')
            {
                if (space is false) sb.Append(' ');
                space = true;
            }
            else
            {
                sb.Append(c);
                space = false;
            }
        }

        return sb.ToString();
    }

    private static bool IsEdgeSpace(char c) => c is ' ' or ' ';

    // Moves white space and line breaks at the edges of a mark out of it, and drops a mark with no text left. A link and
    // code keep theirs: Markdown writes both with their edges as they are.
    private static List<Node> MoveEdgesOut(List<Node> nodes)
    {
        var result = new List<Node>();
        foreach (var node in nodes)
        {
            if (node is not ElementNode element || element.Tag == "br")
            {
                result.Add(node);
                continue;
            }

            if (element.Tag == "code")
            {
                if (PlainText(element.Children).Length > 0) result.Add(element);
                continue;
            }

            var children = MoveEdgesOut(element.Children);
            if (element.Tag == "a")
            {
                if (children.Count > 0) result.Add(new ElementNode("a", element.Href, children));
                continue;
            }

            var before = new List<Node>();
            var after = new List<Node>();

            while (children.Count > 0)
            {
                if (children[0] is ElementNode { Tag: "br" } br)
                {
                    before.Add(br);
                    children.RemoveAt(0);
                }
                else if (children[0] is TextNode first && first.Text.Length > 0 && IsEdgeSpace(first.Text[0]))
                {
                    var lead = first.Text.Length - first.Text.TrimStart(' ', ' ').Length;
                    before.Add(new TextNode(first.Text[..lead]));
                    first.Text = first.Text[lead..];
                    if (first.Text.Length == 0) children.RemoveAt(0);
                }
                else if (children[0] is TextNode { Text.Length: 0 })
                {
                    children.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            while (children.Count > 0)
            {
                if (children[^1] is ElementNode { Tag: "br" } br)
                {
                    after.Insert(0, br);
                    children.RemoveAt(children.Count - 1);
                }
                else if (children[^1] is TextNode last && last.Text.Length > 0 && IsEdgeSpace(last.Text[^1]))
                {
                    var trimmed = last.Text.TrimEnd(' ', ' ');
                    after.Insert(0, new TextNode(last.Text[trimmed.Length..]));
                    last.Text = trimmed;
                    if (last.Text.Length == 0) children.RemoveAt(children.Count - 1);
                }
                else if (children[^1] is TextNode { Text.Length: 0 })
                {
                    children.RemoveAt(children.Count - 1);
                }
                else
                {
                    break;
                }
            }

            result.AddRange(before);
            if (children.Count > 0) result.Add(new ElementNode(element.Tag, element.Href, children));
            result.AddRange(after);
        }

        return result;
    }

    // Joins neighboring text nodes, and neighboring elements of the same mark (and link address), all the way down.
    private static List<Node> Merge(List<Node> nodes)
    {
        var result = new List<Node>();
        foreach (var node in nodes)
        {
            if (node is TextNode text)
            {
                if (text.Text.Length == 0) continue;

                if (result.Count > 0 && result[^1] is TextNode previous)
                {
                    previous.Text += text.Text;
                }
                else
                {
                    result.Add(new TextNode(text.Text));
                }
                continue;
            }

            var element = (ElementNode)node;
            var merged = new ElementNode(element.Tag, element.Href, Merge(element.Children));

            if (merged.Tag != "br"
                && result.Count > 0
                && result[^1] is ElementNode last
                && last.Tag == merged.Tag
                && last.Href == merged.Href)
            {
                last.Children.AddRange(merged.Children);
                result[^1] = new ElementNode(last.Tag, last.Href, Merge(last.Children));
                continue;
            }

            result.Add(merged);
        }

        return result;
    }

    // No space at the start or the end of the block, or next to a line break, and no line break at the very end.
    private static void TrimBlock(List<Node> nodes)
    {
        while (nodes.Count > 0 && nodes[^1] is ElementNode { Tag: "br" }) nodes.RemoveAt(nodes.Count - 1);

        for (var i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] is not TextNode text) continue;

            var atStart = i == 0 || nodes[i - 1] is ElementNode { Tag: "br" };
            var atEnd = i == nodes.Count - 1 || nodes[i + 1] is ElementNode { Tag: "br" };

            if (atStart) text.Text = text.Text.TrimStart(' ');
            if (atEnd) text.Text = text.Text.TrimEnd(' ');
        }

        nodes.RemoveAll(n => n is TextNode { Text.Length: 0 });

        // Dropping a space may have left a line break at the end again.
        while (nodes.Count > 0 && nodes[^1] is ElementNode { Tag: "br" }) nodes.RemoveAt(nodes.Count - 1);
    }

    private static void Write(StringBuilder sb, IEnumerable<Node> nodes, string underlineTag)
    {
        foreach (var node in nodes)
        {
            if (node is TextNode text)
            {
                AppendEncoded(sb, text.Text, attribute: false);
                continue;
            }

            var element = (ElementNode)node;
            if (element.Tag == "br")
            {
                sb.Append("<br>");
                continue;
            }

            var tag = element.Tag == "ins" ? underlineTag : element.Tag;
            sb.Append('<').Append(tag);
            if (element.Href is not null)
            {
                sb.Append(" href=\"");
                AppendEncoded(sb, element.Href, attribute: true);
                sb.Append('"');
            }
            sb.Append('>');
            Write(sb, element.Children, underlineTag);
            sb.Append("</").Append(tag).Append('>');
        }
    }

    private static void AppendPlainText(StringBuilder sb, Node node)
    {
        if (node is TextNode text)
        {
            sb.Append(text.Text);
        }
        else if (node is ElementNode element)
        {
            if (element.Tag == "br")
            {
                sb.Append('\n');
                return;
            }

            foreach (var child in element.Children) AppendPlainText(sb, child);
        }
    }

    private static void AppendEncoded(StringBuilder sb, string text, bool attribute)
    {
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"' when attribute: sb.Append("&quot;"); break;
                case ' ': sb.Append("&nbsp;"); break;
                default: sb.Append(c); break;
            }
        }
    }

    internal abstract class Node;

    internal sealed class TextNode(string text) : Node
    {
        public string Text { get; set; } = text;
    }

    internal sealed class ElementNode(string tag, string? href, List<Node> children) : Node
    {
        public string Tag { get; } = tag;

        public string? Href { get; } = href;

        public List<Node> Children { get; } = children;
    }
}
