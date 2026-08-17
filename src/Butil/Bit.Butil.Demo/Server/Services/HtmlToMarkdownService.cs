using System.Net;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Converts the rendered HTML of a documentation page into Markdown.
/// <para>
/// The docs pages are written for humans: every API-reference row is a component, the live demos
/// add layers of &lt;div&gt;s and controls, and the whole thing carries CSS classes an MCP client
/// has no use for. Handing that HTML to a model burns most of its budget on markup, so pages are
/// flattened here into the Markdown an LLM reads natively - headings, paragraphs, lists, tables and
/// fenced code blocks - which is typically a fifth of the size.
/// </para>
/// </summary>
public static partial class HtmlToMarkdownService
{
    // Elements whose content is presentation-only or already unusable as text.
    private static readonly HashSet<string> _skippedElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "svg", "path", "template", "noscript", "head"
    };

    // Elements that start on their own line and are followed by a blank one.
    private static readonly HashSet<string> _blockElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "address", "article", "aside", "blockquote", "div", "dl", "fieldset", "figure", "footer",
        "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hr", "main", "nav", "ol", "p",
        "pre", "section", "table", "ul"
    };

    public static string ToMarkdown(this string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;

        var document = new HtmlDocument();
        document.LoadHtml(html);

        var builder = new StringBuilder(html.Length / 2);
        WriteChildren(document.DocumentNode, builder, listDepth: 0);

        return Normalize(builder.ToString());
    }

    private static void WriteChildren(HtmlNode node, StringBuilder builder, int listDepth)
    {
        foreach (var child in node.ChildNodes)
        {
            WriteNode(child, builder, listDepth);
        }
    }

    private static void WriteNode(HtmlNode node, StringBuilder builder, int listDepth)
    {
        if (node.NodeType == HtmlNodeType.Comment) return;

        if (node.NodeType == HtmlNodeType.Text)
        {
            AppendText(builder, WebUtility.HtmlDecode(node.InnerText));
            return;
        }

        if (node.NodeType != HtmlNodeType.Element) return;

        var name = node.Name.ToLowerInvariant();

        if (_skippedElements.Contains(name)) return;

        switch (name)
        {
            case "br":
                builder.Append('\n');
                return;

            case "hr":
                AppendBlockBreak(builder);
                builder.Append("---");
                AppendBlockBreak(builder);
                return;

            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                AppendBlockBreak(builder);
                builder.Append('#', name[1] - '0').Append(' ').Append(Inline(node));
                AppendBlockBreak(builder);
                return;

            case "pre":
                AppendCodeBlock(node, builder);
                return;

            case "code" when node.ParentNode is null || node.ParentNode.Name.Equals("pre", StringComparison.OrdinalIgnoreCase) is false:
                AppendText(builder, $"`{Inline(node)}`", collapse: false);
                return;

            case "strong" or "b":
                AppendWrapped(node, builder, "**");
                return;

            case "em" or "i":
                AppendWrapped(node, builder, "_");
                return;

            case "a":
                AppendLink(node, builder);
                return;

            case "img":
                var alt = node.GetAttributeValue("alt", string.Empty);
                if (string.IsNullOrWhiteSpace(alt) is false) AppendText(builder, $"[image: {alt}]");
                return;

            case "ul" or "ol":
                AppendList(node, builder, listDepth);
                return;

            case "table":
                AppendTable(node, builder);
                return;

            case "dl":
                AppendDefinitionList(node, builder, listDepth);
                return;

            case "button" or "input" or "select" or "textarea" or "option":
                // The live demos are the point of these pages for a human and noise for a reader:
                // "Run the check" and an empty text box say nothing about the API.
                return;
        }

        var isBlock = _blockElements.Contains(name);

        if (isBlock) AppendBlockBreak(builder);

        WriteChildren(node, builder, listDepth);

        if (isBlock) AppendBlockBreak(builder);
    }

    private static void AppendWrapped(HtmlNode node, StringBuilder builder, string marker)
    {
        var content = Inline(node);
        if (content.Length == 0) return;

        AppendText(builder, $"{marker}{content}{marker}", collapse: false);
    }

    private static void AppendLink(HtmlNode node, StringBuilder builder)
    {
        var text = Inline(node);
        if (text.Length == 0) return;

        // The "#" every section heading carries is a link for a mouse, and reads as a stray
        // character in front of the heading it belongs to.
        if (text == "#") return;

        var href = node.GetAttributeValue("href", string.Empty);

        // A link whose text already is its target (the docs link to their own routes that way)
        // reads better as plain text than as a Markdown link that repeats itself.
        if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#') || string.Equals(href, text, StringComparison.OrdinalIgnoreCase))
        {
            AppendText(builder, text, collapse: false);
            return;
        }

        AppendText(builder, $"[{text}]({WebUtility.HtmlDecode(href)})", collapse: false);
    }

    private static void AppendCodeBlock(HtmlNode node, StringBuilder builder)
    {
        // The highlighting spans carry no information: their text content already is the code.
        var code = WebUtility.HtmlDecode(node.InnerText).Trim('\n', '\r');
        if (code.Trim().Length == 0) return;

        // The fence has to outrun the longest run of backticks in the sample itself - the docs do
        // show Markdown - or the block would end in the middle of its own content.
        var longest = BacktickRunRegex().Matches(code).Select(match => match.Length).DefaultIfEmpty(0).Max();
        var fence = new string('`', Math.Max(3, longest + 1));

        AppendBlockBreak(builder);
        builder.Append(fence).Append('\n').Append(code).Append('\n').Append(fence);
        AppendBlockBreak(builder);
    }

    private static void AppendList(HtmlNode node, StringBuilder builder, int listDepth)
    {
        var ordered = node.Name.Equals("ol", StringComparison.OrdinalIgnoreCase);
        var index = 1;

        AppendBlockBreak(builder);

        foreach (var item in node.ChildNodes.Where(n => n.Name.Equals("li", StringComparison.OrdinalIgnoreCase)))
        {
            TrimTrailingWhitespace(builder);
            if (builder.Length > 0) builder.Append('\n');

            builder.Append(new string(' ', listDepth * 2))
                   .Append(ordered ? $"{index++}. " : "- ");

            var content = new StringBuilder();
            WriteChildren(item, content, listDepth + 1);

            // Keep a nested list attached to its parent item, but never leave a blank line inside one.
            builder.Append(Normalize(content.ToString()).Replace("\n\n", "\n", StringComparison.Ordinal));
        }

        AppendBlockBreak(builder);
    }

    private static void AppendDefinitionList(HtmlNode node, StringBuilder builder, int listDepth)
    {
        AppendBlockBreak(builder);

        foreach (var child in node.ChildNodes)
        {
            if (child.Name.Equals("dt", StringComparison.OrdinalIgnoreCase))
            {
                TrimTrailingWhitespace(builder);
                if (builder.Length > 0) builder.Append('\n');
                builder.Append("- **").Append(Inline(child)).Append("**");
            }
            else if (child.Name.Equals("dd", StringComparison.OrdinalIgnoreCase))
            {
                var value = Inline(child);
                if (value.Length > 0) builder.Append(": ").Append(value);
            }
        }

        AppendBlockBreak(builder);
    }

    private static void AppendTable(HtmlNode node, StringBuilder builder)
    {
        // Descendants() reaches into a nested table too, and its rows belong to that table - which
        // renders itself when the cell holding it is written out.
        var rows = node.Descendants()
                       .Where(n => n.Name.Equals("tr", StringComparison.OrdinalIgnoreCase) && OwningTable(n) == node)
                       .Select(tr => tr.ChildNodes
                                       .Where(c => c.Name.Equals("th", StringComparison.OrdinalIgnoreCase) ||
                                                   c.Name.Equals("td", StringComparison.OrdinalIgnoreCase))
                                       .Select(c => Inline(c).Replace("|", "\\|", StringComparison.Ordinal))
                                       .ToArray())
                       .Where(cells => cells.Length > 0)
                       .ToArray();

        if (rows.Length == 0) return;

        var columns = rows.Max(r => r.Length);

        AppendBlockBreak(builder);

        for (int i = 0; i < rows.Length; i++)
        {
            var cells = rows[i];
            builder.Append("| ");
            for (int c = 0; c < columns; c++)
            {
                builder.Append(c < cells.Length ? cells[c] : string.Empty).Append(" | ");
            }
            builder.Append('\n');

            // Markdown needs the delimiter row right after the first one, header or not.
            if (i == 0)
            {
                builder.Append("| ");
                for (int c = 0; c < columns; c++) builder.Append("--- | ");
                builder.Append('\n');
            }
        }

        AppendBlockBreak(builder);
    }

    /// <summary>The innermost table a row sits in.</summary>
    private static HtmlNode? OwningTable(HtmlNode row)
    {
        for (var parent = row.ParentNode; parent is not null; parent = parent.ParentNode)
        {
            if (parent.Name.Equals("table", StringComparison.OrdinalIgnoreCase)) return parent;
        }

        return null;
    }

    /// <summary>Renders a node's content as a single line, for headings and table/list cells.</summary>
    private static string Inline(HtmlNode node)
    {
        var builder = new StringBuilder();
        WriteChildren(node, builder, listDepth: 0);

        return WhitespaceRegex().Replace(builder.ToString(), " ").Trim();
    }

    private static void AppendText(StringBuilder builder, string text, bool collapse = true)
    {
        if (collapse)
        {
            text = WhitespaceRegex().Replace(text, " ");
            if (text.Trim().Length == 0)
            {
                // A single separating space still matters between two inline elements.
                if (text.Length > 0 && builder.Length > 0 && char.IsWhiteSpace(builder[^1]) is false) builder.Append(' ');
                return;
            }
        }

        // Never start a line with the whitespace that only existed to indent the HTML source.
        if (builder.Length == 0 || builder[^1] == '\n') text = text.TrimStart();

        builder.Append(text);
    }

    private static void AppendBlockBreak(StringBuilder builder)
    {
        if (builder.Length == 0) return;

        TrimTrailingWhitespace(builder);
        builder.Append("\n\n");
    }

    private static void TrimTrailingWhitespace(StringBuilder builder)
    {
        while (builder.Length > 0 && char.IsWhiteSpace(builder[^1])) builder.Length--;
    }

    private static string Normalize(string markdown)
    {
        markdown = markdown.Replace("\r\n", "\n", StringComparison.Ordinal);
        // Only the spaces go - the newline that ended the line has to stay, or every table row
        // (each of which ends in the "| " a cell separator leaves behind) folds into its neighbour.
        markdown = TrailingSpacesRegex().Replace(markdown, "\n");
        markdown = BlankLinesRegex().Replace(markdown, "\n\n");

        return markdown.Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[ \t]+\n")]
    private static partial Regex TrailingSpacesRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex BlankLinesRegex();

    [GeneratedRegex("`+")]
    private static partial Regex BacktickRunRegex();
}
