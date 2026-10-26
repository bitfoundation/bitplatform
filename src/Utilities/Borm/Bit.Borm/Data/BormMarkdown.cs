using System.Net;
using System.Text;

namespace Bit.Borm;

/// <summary>
/// Turns the Markdown a <see cref="BormFieldType.Markdown"/> field holds into HTML that is safe to put on a page:
/// paragraphs and line breaks, headings, bulleted and numbered lists, quotes, code, rules, <c>**bold**</c>,
/// <c>*italic*</c>, <c>~~struck~~</c>, <c>`code`</c> and <c>[links](https://…)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Everything the user wrote is HTML-encoded, and the only elements written are the ones listed above, so no
/// markup the user types - a <c>&lt;script&gt;</c>, an <c>onerror</c> attribute, an entity - reaches the page
/// as markup. A link is written only to an <c>http</c>, <c>https</c> or <c>mailto</c> address; anything else,
/// <c>javascript:</c> among it, stays text. Links carry <c>rel="nofollow noopener noreferrer"</c>.
/// </para>
/// <para>
/// Parsing is linear in the length of the text whatever it holds, and nesting - a quote in a quote, emphasis in
/// emphasis - is bounded, so a hostile value cannot make rendering it expensive.
/// </para>
/// </remarks>
public static class BormMarkdown
{
    private const int MaxDepth = 4;

    /// <summary>The HTML for <paramref name="markdown"/>; empty for no text.</summary>
    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        var html = new StringBuilder(markdown.Length + markdown.Length / 4);
        Blocks(markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'), html, depth: 0);
        return html.ToString();
    }

    /// <summary>The text of <paramref name="markdown"/> with the formatting taken out - for a summary, a review, a search.</summary>
    public static string ToPlainText(string? markdown)
    {
        var html = ToHtml(markdown);
        if (html.Length == 0) return string.Empty;

        var text = new StringBuilder(html.Length);
        var inTag = false;
        foreach (var c in html)
        {
            if (c == '<') inTag = true;
            else if (c == '>')
            {
                inTag = false;
                text.Append(' ');
            }
            else if (!inTag) text.Append(c);
        }
        return string.Join(' ', WebUtility.HtmlDecode(text.ToString()).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void Blocks(string[] lines, StringBuilder html, int depth)
    {
        var paragraph = new List<string>();
        var i = 0;
        while (i < lines.Length)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0)
            {
                Paragraph();
                i++;
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                Paragraph();
                var code = new List<string>();
                for (i++; i < lines.Length && !lines[i].TrimStart().StartsWith("```", StringComparison.Ordinal); i++) code.Add(lines[i]);
                i++;
                html.Append("<pre><code>").Append(Encode(string.Join('\n', code))).Append("</code></pre>");
                continue;
            }

            if (Heading(trimmed, out var level, out var title))
            {
                Paragraph();
                // Inside a form: the field's own label is the heading the page gives it, so the text's headings
                // start below it.
                var tag = "h" + Math.Min(6, level + 2);
                html.Append('<').Append(tag).Append('>');
                Inline(title, html, 0);
                html.Append("</").Append(tag).Append('>');
                i++;
                continue;
            }

            if (IsRule(trimmed))
            {
                Paragraph();
                html.Append("<hr>");
                i++;
                continue;
            }

            if (trimmed[0] == '>' && depth < MaxDepth)
            {
                Paragraph();
                var quote = new List<string>();
                for (; i < lines.Length && lines[i].TrimStart() is { Length: > 0 } q && q[0] == '>'; i++) quote.Add(q[1..]);
                html.Append("<blockquote>");
                Blocks([.. quote], html, depth + 1);
                html.Append("</blockquote>");
                continue;
            }

            if (ListItem(trimmed, out var ordered, out _))
            {
                Paragraph();
                var tag = ordered ? "ol" : "ul";
                html.Append('<').Append(tag).Append('>');
                for (; i < lines.Length && ListItem(lines[i].Trim(), out var o, out var item) && o == ordered; i++)
                {
                    html.Append("<li>");
                    Inline(item, html, 0);
                    html.Append("</li>");
                }
                html.Append("</").Append(tag).Append('>');
                continue;
            }

            paragraph.Add(trimmed);
            i++;
        }
        Paragraph();

        // A line break inside a paragraph is kept: in a form, a user who pressed Enter meant it.
        void Paragraph()
        {
            if (paragraph.Count == 0) return;
            html.Append("<p>");
            for (var p = 0; p < paragraph.Count; p++)
            {
                if (p > 0) html.Append("<br>");
                Inline(paragraph[p], html, 0);
            }
            html.Append("</p>");
            paragraph.Clear();
        }
    }

    private static bool Heading(string line, out int level, out string text)
    {
        level = 0;
        while (level < line.Length && line[level] == '#') level++;
        text = level < line.Length ? line[level..].Trim() : string.Empty;
        return level is >= 1 and <= 6 && level < line.Length && line[level] == ' ';
    }

    private static bool IsRule(string line)
    {
        var marker = line[0];
        if (marker is not ('-' or '*' or '_')) return false;
        var count = 0;
        foreach (var c in line)
        {
            if (c == marker) count++;
            else if (c != ' ') return false;
        }
        return count >= 3;
    }

    private static bool ListItem(string line, out bool ordered, out string text)
    {
        ordered = false;
        text = string.Empty;
        if (line.Length >= 2 && line[0] is ('-' or '*' or '+') && line[1] == ' ')
        {
            text = line[2..].Trim();
            return true;
        }

        var digits = 0;
        while (digits < line.Length && digits < 9 && char.IsAsciiDigit(line[digits])) digits++;
        if (digits == 0 || digits + 1 >= line.Length || line[digits] is not ('.' or ')') || line[digits + 1] != ' ') return false;
        ordered = true;
        text = line[(digits + 2)..].Trim();
        return true;
    }

    private static void Inline(string text, StringBuilder html, int depth)
    {
        // Where a closing marker was last looked for and not found. Any later search for the same marker
        // would scan a part of what that one scanned, and fail too - so it is not made, and a line of a
        // thousand unmatched asterisks costs one scan rather than a thousand.
        Dictionary<string, int>? missing = null;
        // Where each marker was last found, and where that search started: there is none between the two, so a
        // search starting anywhere in that stretch ends in the same place without scanning it again. A thousand
        // "[" before one "](" that leads nowhere safe otherwise each scanned all the way to it.
        Dictionary<string, (int From, int At)>? found = null;
        // The address after the last "](" judged, and the verdict - the same for every "[" in front of it.
        var judgedAt = -1;
        string? judgedUrl = null;

        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];

            if (c == '\\' && i + 1 < text.Length && (char.IsPunctuation(text[i + 1]) || char.IsSymbol(text[i + 1])))
            {
                html.Append(Encode(text[i + 1].ToString()));
                i += 2;
                continue;
            }

            if (c == '`' && Close("`", i + 1) is { } codeEnd)
            {
                html.Append("<code>").Append(Encode(text[(i + 1)..codeEnd])).Append("</code>");
                i = codeEnd + 1;
                continue;
            }

            if (depth < MaxDepth)
            {
                if (Span("**", "strong") || Span("__", "strong") || Span("~~", "del") || Span("*", "em") || Span("_", "em")) continue;
                if (c == '[' && Link()) continue;
            }

            html.Append(Encode(c.ToString()));
            i++;
        }

        int? Close(string marker, int from)
        {
            if (from >= text.Length) return null;
            if (missing is not null && missing.TryGetValue(marker, out var failedFrom) && from >= failedFrom) return null;
            if (found is not null && found.TryGetValue(marker, out var last) && from >= last.From && from <= last.At) return last.At;
            var at = text.IndexOf(marker, from, StringComparison.Ordinal);
            if (at >= 0)
            {
                (found ??= [])[marker] = (from, at);
                return at;
            }
            (missing ??= [])[marker] = from;
            return null;
        }

        bool Span(string marker, string tag)
        {
            if (string.CompareOrdinal(text, i, marker, 0, marker.Length) != 0) return false;
            var start = i + marker.Length;
            // "* " opens nothing: a lone asterisk between words is an asterisk.
            if (start >= text.Length || text[start] == ' ') return false;
            if (Close(marker, start + 1) is not { } end || text[end - 1] == ' ') return false;

            html.Append('<').Append(tag).Append('>');
            Inline(text[start..end], html, depth + 1);
            html.Append("</").Append(tag).Append('>');
            i = end + marker.Length;
            return true;
        }

        bool Link()
        {
            if (Close("](", i + 1) is not { } middle || middle == i + 1 || Close(")", middle + 2) is not { } end) return false;
            // Judged once per address, and the label cut out only for a link that is one: copied out for every "["
            // that leads to an address refused, it was the whole text again each time.
            if (middle != judgedAt)
            {
                judgedAt = middle;
                var address = text[(middle + 2)..end].Trim();
                judgedUrl = IsSafeUrl(address) ? address : null;
            }
            if (judgedUrl is not { } url) return false;

            html.Append("<a href=\"").Append(Encode(url)).Append("\" rel=\"nofollow noopener noreferrer\">");
            Inline(text[(i + 1)..middle], html, depth + 1);
            html.Append("</a>");
            i = end + 1;
            return true;
        }
    }

    /// <summary>An address a link may go to: http, https or mailto, with nothing in it that is not part of one.</summary>
    private static bool IsSafeUrl(string url)
    {
        if (url.Length == 0 || url.Length > 2048) return false;
        foreach (var c in url)
            if (char.IsControl(c) || char.IsWhiteSpace(c) || c is '<' or '>' or '"' or '\'' or '`') return false;

        return url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
