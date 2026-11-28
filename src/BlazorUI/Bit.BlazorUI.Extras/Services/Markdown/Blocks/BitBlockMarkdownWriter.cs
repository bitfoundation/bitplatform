using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Writes a block document as Markdown that <see cref="BitBlockMarkdownReader"/> reads back into the same blocks.
/// </summary>
/// <remarks>
/// <para>
/// Blocks are separated by a blank line, which ends every container before the next block starts, and the items of
/// one list by a newline. A list item's children are indented under its text by the width of its marker.
/// </para>
/// <para>
/// Text is escaped wherever it would otherwise read as syntax: the delimiters of every mark and of links and code
/// always, and what opens a block (<c>#</c>, <c>-</c>, <c>+</c>, <c>=</c>, <c>1.</c>) at the start of a line. A mark
/// is written as <c>**</c>, <c>*</c>, <c>~~</c>, <c>`</c>, <c>[]()</c>, <c>~</c>, <c>^</c>, <c>==</c> and <c>++</c>;
/// italic is written as <c>_</c> where it meets bold at an edge, so the two delimiters never run together.
/// </para>
/// </remarks>
internal static partial class BitBlockMarkdownWriter
{
    public static string Write(IReadOnlyList<BitBlock> blocks, Dictionary<string, BitBlockType> registry)
        => WriteBlocks(blocks, registry);

    private static string WriteBlocks(IReadOnlyList<BitBlock> blocks, Dictionary<string, BitBlockType> registry)
    {
        var sb = new StringBuilder();
        string? previousList = null;
        var ordinal = 0;

        foreach (var block in blocks)
        {
            var type = BitBlockTypes.ExportType(block.Type, registry, t => t.ToMarkdown);
            var isList = type?.ToMarkdown is null && ReferenceEquals(type, BitBlockTypes.ListItem);
            var list = isList ? BitBlockTypes.ListStyle(block) : null;

            // An item of the list before it follows it on the next line, and a task item continues a bulleted list
            // (both are written with "-"); everything else starts after a blank line.
            var continues = list is not null && previousList is not null && (list == previousList || (list != "ordered" && previousList != "ordered"));
            ordinal = continues && list == "ordered" ? ordinal + 1 : 1;

            var written = isList
                ? WriteListItem(block, list!, ordinal, registry)
                : WriteBlock(block, type, registry);

            previousList = list;
            if (written is null) continue;

            if (sb.Length > 0) sb.Append(continues ? "\n" : "\n\n");
            sb.Append(written);
        }

        return sb.ToString();
    }

    private static string? WriteBlock(BitBlock block, BitBlockType? type, Dictionary<string, BitBlockType> registry)
    {
        if (type?.ToMarkdown is { } toMarkdown)
        {
            return toMarkdown(block, WriteBlocks(block.Children, registry));
        }

        if (ReferenceEquals(type, BitBlockTypes.Paragraph))
        {
            var text = Inline(block.Text, heading: false);
            // Markdown has no empty paragraph; an empty one is left out.
            return text.Length == 0 ? null : text;
        }

        if (ReferenceEquals(type, BitBlockTypes.Heading))
        {
            var text = Inline(block.Text, heading: true);
            return new string('#', BitBlockTypes.HeadingLevel(block)) + (text.Length == 0 ? "" : " " + text);
        }

        if (ReferenceEquals(type, BitBlockTypes.Quote))
        {
            var text = Inline(block.Text, heading: false);
            return text.Length == 0 ? ">" : Prefix(text, "> ", ">");
        }

        if (ReferenceEquals(type, BitBlockTypes.Code)) return CodeFence(block.Text, block.GetProp("language"));

        if (ReferenceEquals(type, BitBlockTypes.Image)) return Image(block);

        if (ReferenceEquals(type, BitBlockTypes.Divider)) return "---";

        // An app's type with no export of its own, or a type the registry does not know: its text as a paragraph, and
        // its children after it as blocks of their own.
        var own = type?.Kind switch
        {
            BitBlockKind.Atom => "",
            BitBlockKind.Code => string.Join("\\\n", block.Text.Replace("\r\n", "\n").Split('\n').Select(l => EscapeText(l.Trim(), lineStart: true, heading: false))),
            _ => Inline(block.Text, heading: false)
        };
        var children = WriteBlocks(block.Children, registry);

        if (own.Length == 0) return children.Length == 0 ? null : children;
        return children.Length == 0 ? own : own + "\n\n" + children;
    }

    private static string WriteListItem(BitBlock block, string style, int ordinal, Dictionary<string, BitBlockType> registry)
    {
        var marker = style switch
        {
            "ordered" => ordinal.ToString(CultureInfo.InvariantCulture) + ".",
            _ => "-"
        };
        var indent = new string(' ', marker.Length + 1);

        var text = Inline(block.Text, heading: false);
        if (style == "task") text = (BitBlockTypes.IsChecked(block) ? "[x]" : "[ ]") + (text.Length == 0 ? "" : " " + text);

        var sb = new StringBuilder(marker);
        if (text.Length > 0) sb.Append(' ').Append(Prefix(text, indent, "", firstPrefix: ""));

        if (block.Children.Count > 0)
        {
            var children = WriteBlocks(block.Children, registry);
            if (children.Length > 0)
            {
                // A nested list follows the text on the next line, and so does anything under an item with no text (an
                // item may open with one blank line, never two); any other child is a block of the item, after a blank
                // line.
                var nestedList = block.Children[0].Type == BitBlockTypes.ListItem.Name;
                sb.Append(nestedList || text.Length == 0 ? "\n" : "\n\n");
                sb.Append(Prefix(children, indent, ""));
            }
        }

        return sb.ToString();
    }

    // Prefixes every line: non-empty lines with the prefix, empty ones with the empty prefix (an empty line of a quote is
    // ">", of a list item nothing). The first line takes firstPrefix when one is given.
    private static string Prefix(string text, string prefix, string emptyPrefix, string? firstPrefix = null)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.Append('\n');
            var p = i == 0 && firstPrefix is not null ? firstPrefix : prefix;
            sb.Append(lines[i].Length == 0 ? emptyPrefix : p + lines[i]);
        }

        return sb.ToString();
    }

    private static string CodeFence(string code, string? language)
    {
        code = code.Replace("\r\n", "\n").Replace('\r', '\n');

        // A fence longer than any run of backticks the code holds, so no line of it can close the block.
        var longest = 0;
        var run = 0;
        foreach (var c in code)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        var fence = new string('`', Math.Max(3, longest + 1));
        var info = BitBlockHtmlConverter.Language(language) ?? "";

        return code.Length == 0
            ? $"{fence}{info}\n{fence}"
            : $"{fence}{info}\n{code}\n{fence}";
    }

    private static string? Image(BitBlock block)
    {
        var src = block.GetProp("src") ?? "";
        // An address the Markdown reader would refuse is left out with its image, rather than written for someone else
        // to render.
        if (src.Length > 0 && BitMarkdownUrlSanitizer.Sanitize(src, isImage: true).Length == 0) return null;

        var alt = EscapeText(Collapse(block.GetProp("alt") ?? ""), lineStart: false, heading: false);
        var caption = block.GetProp("caption");
        var title = string.IsNullOrEmpty(caption) ? "" : $" \"{EncodeReferences(caption).Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

        return $"![{alt}]({Destination(src)}{title})";
    }

    private static string Collapse(string text) => string.Join(' ', text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries));

    // A link destination as written inside the parentheses: bare when it can be, in angle brackets when it holds a
    // space, a parenthesis or an angle bracket, or is empty. The reader decodes character references in both, so an
    // '&' that would start one is written as one; it reads backslash escapes in a bare destination only, so inside
    // angle brackets a bracket is written as a character reference too and a backslash as it is.
    private static string Destination(string url)
    {
        var encoded = EncodeReferences(url);

        if (url.Length > 0 && url.Any(c => c is ' ' or '(' or ')' or '<' or '>' or '\n' or '\r') is false)
        {
            return encoded.Replace("\\", "\\\\");
        }

        return "<" + encoded.Replace("<", "&lt;").Replace(">", "&gt;").Replace("\n", " ").Replace("\r", " ") + ">";
    }

    // Writes as "&amp;" every '&' that starts what reads as a character reference, so the reader's decoding gives the
    // text back.
    private static string EncodeReferences(string text)
        => text.Contains('&') ? CharacterReference().Replace(text, m => "&amp;" + m.Value[1..]) : text;

    [GeneratedRegex("&#?[A-Za-z0-9]+;")]
    private static partial Regex CharacterReference();



    // The block's text, read into the canonical tree, as Markdown. A heading holds one line, so its line breaks are
    // written as spaces.
    private static string Inline(string html, bool heading)
    {
        var nodes = BitBlockInline.Parse(html);
        var writer = new InlineWriter(heading);
        writer.Write(nodes, parent: null);
        return writer.ToString();
    }

    private sealed class InlineWriter(bool heading)
    {
        private readonly StringBuilder _sb = new();
        private bool _lineStart = true;

        public override string ToString() => _sb.ToString();

        public void Write(List<BitBlockInline.Node> nodes, BitBlockInline.ElementNode? parent)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is BitBlockInline.TextNode text)
                {
                    Append(EscapeText(text.Text, _lineStart, heading));
                    continue;
                }

                var element = (BitBlockInline.ElementNode)node;
                switch (element.Tag)
                {
                    case "br":
                        if (heading)
                        {
                            Append(" ");
                        }
                        else
                        {
                            _sb.Append("\\\n");
                            _lineStart = true;
                        }
                        break;

                    case "code":
                        Append(CodeSpan(BitBlockInline.PlainText(element.Children)));
                        break;

                    case "a":
                        // A '!' of the text right before the link would make it an image. The text never writes one
                        // escaped, so one at the end is always bare.
                        if (_sb.Length > 0 && _sb[^1] == '!') _sb.Insert(_sb.Length - 1, '\\');
                        Append("[");
                        Write(element.Children, element);
                        Append("](" + Destination(element.Href ?? "") + ")");
                        break;

                    default:
                        var delimiter = Delimiter(element, parent, isEdge: i == 0 || i == nodes.Count - 1);
                        Append(delimiter);
                        Write(element.Children, element);
                        Append(delimiter);
                        break;
                }
            }
        }

        private void Append(string text)
        {
            if (text.Length == 0) return;
            _sb.Append(text);
            _lineStart = false;
        }

        private static string Delimiter(BitBlockInline.ElementNode element, BitBlockInline.ElementNode? parent, bool isEdge)
            => element.Tag switch
            {
                "b" => "**",
                // Italic meeting bold at an edge takes the other delimiter, so "**" and "*" never run together into a
                // run the reader would split differently.
                "i" => (parent?.Tag == "b" && isEdge)
                       || element.Children.FirstOrDefault() is BitBlockInline.ElementNode { Tag: "b" }
                       || element.Children.LastOrDefault() is BitBlockInline.ElementNode { Tag: "b" }
                    ? "_"
                    : "*",
                "s" => "~~",
                "sub" => "~",
                "sup" => "^",
                "mark" => "==",
                "ins" => "++",
                _ => ""
            };
    }

    // A code span: a run of backticks longer than any its content holds, and a space inside each end when the content
    // starts or ends with a backtick, or with a space at both ends (which the reader strips one of).
    private static string CodeSpan(string content)
    {
        var longest = 0;
        var run = 0;
        foreach (var c in content)
        {
            run = c == '`' ? run + 1 : 0;
            longest = Math.Max(longest, run);
        }

        var fence = new string('`', longest + 1);
        var pad = content.StartsWith('`') || content.EndsWith('`')
            || (content.Length > 1 && content[0] == ' ' && content[^1] == ' ' && content.Any(c => c != ' '));

        return pad ? $"{fence} {content} {fence}" : $"{fence}{content}{fence}";
    }

    // Escapes text wherever it would read as syntax. The marks' delimiters, link brackets, backslashes, backticks,
    // angle brackets and ampersands always; "+" and "=" where two would make a delimiter; what opens a block at the start
    // of a line; and "#" anywhere in a heading, whose closing run of them the reader strips.
    internal static string EscapeText(string text, bool lineStart, bool heading)
    {
        var sb = new StringBuilder(text.Length + 8);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var atLineStart = lineStart && i == 0;

            var escape = c switch
            {
                '\\' or '`' or '*' or '_' or '[' or ']' or '<' or '>' or '~' or '^' or '&' => true,
                '+' or '=' => i == 0 || i == text.Length - 1 || text[i - 1] == c || text[i + 1] == c || atLineStart,
                '#' => heading || atLineStart,
                '-' => atLineStart,
                '.' or ')' => lineStart && i > 0 && IsDigits(text, 0, i),
                _ => false
            };

            if (escape) sb.Append('\\');
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsDigits(string text, int start, int end)
    {
        if (end - start is < 1 or > 9) return false;
        for (var i = start; i < end; i++)
        {
            if (char.IsAsciiDigit(text[i]) is false) return false;
        }

        return true;
    }
}
