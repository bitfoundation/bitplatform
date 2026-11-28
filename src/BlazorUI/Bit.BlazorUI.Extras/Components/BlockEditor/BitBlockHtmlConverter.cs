using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI;

/// <summary>
/// Writes a block document as the HTML a <see cref="BitRichTextEditor"/> stores, and reads that HTML (or any other)
/// back into blocks.
/// </summary>
internal static partial class BitBlockHtmlConverter
{
    private static readonly HashSet<string> InlineTags = ["a", "b", "br", "code", "em", "i", "mark", "s", "span", "strike", "strong", "sub", "sup", "u"];

    private static readonly HashSet<string> HeadingTags = ["h1", "h2", "h3", "h4", "h5", "h6"];

    private static readonly HashSet<string> MediaTags = ["audio", "iframe", "video"];

    public static string Write(IReadOnlyList<BitBlock> blocks, Dictionary<string, BitBlockType> registry)
    {
        var sb = new StringBuilder();
        foreach (var unit in Units(blocks))
        {
            // Each block (each list) is sanitized on its own, so an app's export of one cannot reach into another, and
            // no single call nears the sanitizer's input limit however long the document is.
            sb.Append(BitRichTextEditorHtmlSanitizer.Sanitize(WriteUnit(unit, registry), null));
        }

        return sb.ToString();
    }

    public static IReadOnlyList<BitBlock> Read(string? html)
    {
        var tree = BitBlockHtmlTree.Parse(html, null);
        return ReadBlocks(tree.Children);
    }

    // The blocks in runs that are written together: consecutive list items of one style, or any other block on its own.
    private static IEnumerable<List<BitBlock>> Units(IReadOnlyList<BitBlock> blocks)
    {
        List<BitBlock>? list = null;
        foreach (var block in blocks)
        {
            if (block.Type == BitBlockTypes.ListItem.Name)
            {
                if (list is not null && BitBlockTypes.ListStyle(list[0]) == BitBlockTypes.ListStyle(block))
                {
                    list.Add(block);
                    continue;
                }

                if (list is not null) yield return list;
                list = [block];
                continue;
            }

            if (list is not null)
            {
                yield return list;
                list = null;
            }

            yield return [block];
        }

        if (list is not null) yield return list;
    }

    private static string WriteUnit(List<BitBlock> unit, Dictionary<string, BitBlockType> registry)
    {
        var first = unit[0];
        var type = BitBlockTypes.ExportType(first.Type, registry, t => t.ToHtml);

        // An app's type, or an app's replacement of a built-in, writes itself.
        if (type?.ToHtml is { } toHtml)
        {
            var sb = new StringBuilder();
            foreach (var block in unit) sb.Append(toHtml(block, WriteChildren(block, registry)));
            return sb.ToString();
        }

        if (ReferenceEquals(type, BitBlockTypes.ListItem)) return WriteList(unit, registry);

        return WriteBuiltIn(first, type, registry);
    }

    private static string WriteChildren(BitBlock block, Dictionary<string, BitBlockType> registry)
        => block.Children.Count == 0 ? "" : string.Concat(Units(block.Children).Select(u => WriteUnit(u, registry)));

    private static string WriteList(List<BitBlock> items, Dictionary<string, BitBlockType> registry)
    {
        var style = BitBlockTypes.ListStyle(items[0]);
        var tag = style == "ordered" ? "ol" : "ul";

        var sb = new StringBuilder();
        sb.Append('<').Append(tag).Append('>');
        foreach (var item in items)
        {
            sb.Append(style == "task"
                ? $"<li data-checked=\"{(BitBlockTypes.IsChecked(item) ? "true" : "false")}\">"
                : "<li>");
            sb.Append(BitBlockInline.WriteHtml(BitBlockInline.Parse(item.Text)));
            sb.Append(WriteChildren(item, registry));
            sb.Append("</li>");
        }
        sb.Append("</").Append(tag).Append('>');
        return sb.ToString();
    }

    private static string WriteBuiltIn(BitBlock block, BitBlockType? type, Dictionary<string, BitBlockType> registry)
    {
        var inline = type?.Kind == BitBlockKind.Text || type is null
            ? BitBlockInline.WriteHtml(BitBlockInline.Parse(block.Text))
            : "";

        if (ReferenceEquals(type, BitBlockTypes.Paragraph)) return $"<p>{Placeholder(inline)}</p>";

        if (ReferenceEquals(type, BitBlockTypes.Heading))
        {
            var level = BitBlockTypes.HeadingLevel(block).ToString(CultureInfo.InvariantCulture);
            return $"<h{level}>{inline}</h{level}>";
        }

        if (ReferenceEquals(type, BitBlockTypes.Quote)) return $"<blockquote><p>{Placeholder(inline)}</p></blockquote>";

        if (ReferenceEquals(type, BitBlockTypes.Code))
        {
            var language = Language(block.GetProp("language"));
            var attribute = language is null ? "" : $" class=\"language-{language}\"";
            return $"<pre><code{attribute}>{BitBlockInline.Encode(block.Text)}</code></pre>";
        }

        if (ReferenceEquals(type, BitBlockTypes.Image)) return $"<p>{ImageTag(block)}</p>";

        if (ReferenceEquals(type, BitBlockTypes.Divider)) return "<hr>";

        // An app's type with no export of its own, or a type the registry does not know: its text as a paragraph, and
        // its children after it.
        var text = type?.Kind switch
        {
            BitBlockKind.Code => BitBlockInline.Encode(block.Text).Replace("\n", "<br>"),
            BitBlockKind.Atom => "",
            _ => inline
        };

        return (text.Length > 0 ? $"<p>{text}</p>" : "") + WriteChildren(block, registry);
    }

    // An empty paragraph is a line break, which is how the browser keeps an empty line open (and how the
    // RichTextEditor stores one).
    private static string Placeholder(string inline) => inline.Length == 0 ? "<br>" : inline;

    private static string ImageTag(BitBlock block)
    {
        var sb = new StringBuilder("<img");
        Attribute(sb, "src", block.GetProp("src") ?? "");
        Attribute(sb, "alt", block.GetProp("alt") ?? "");
        if (block.GetProp("width") is { } width && int.TryParse(width, NumberStyles.None, CultureInfo.InvariantCulture, out var pixels))
        {
            Attribute(sb, "width", pixels.ToString(CultureInfo.InvariantCulture));
        }
        if (block.GetProp("caption") is { Length: > 0 } caption) Attribute(sb, "title", caption);
        sb.Append('>');
        return sb.ToString();
    }

    private static void Attribute(StringBuilder sb, string name, string value)
        => sb.Append(' ').Append(name).Append("=\"").Append(BitBlockInline.EncodeAttribute(value)).Append('"');

    // A code block's language as a class name can carry it: letters, digits and a few of the punctuation marks language
    // names use (c++, c#, objective-c, ...).
    internal static string? Language(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return null;

        var match = LanguageName().Match(language.Trim());
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_+#.-]+")]
    private static partial Regex LanguageName();



    private static List<BitBlock> ReadBlocks(IEnumerable<BitBlockHtmlTree> nodes)
    {
        var blocks = new List<BitBlock>();
        var run = new List<BitBlockHtmlTree>();

        void Flush()
        {
            if (run.Count == 0) return;
            if (run.Any(n => n.IsText is false || string.IsNullOrWhiteSpace(n.Text) is false))
            {
                blocks.AddRange(Paragraphs(run, BitBlockTypes.Paragraph.Name));
            }
            run.Clear();
        }

        foreach (var node in nodes)
        {
            if (node.IsText || InlineTags.Contains(node.Tag!) || node.Tag == "img")
            {
                run.Add(node);
                continue;
            }

            Flush();
            var tag = node.Tag!;

            if (tag == "p")
            {
                blocks.AddRange(Paragraphs(node.Children, BitBlockTypes.Paragraph.Name));
            }
            else if (HeadingTags.Contains(tag))
            {
                blocks.Add(new BitBlock
                {
                    Type = BitBlockTypes.Heading.Name,
                    Text = InlineText(node.Children),
                    Props = Props(("level", tag[1..]))
                });
            }
            else if (tag is "ul" or "ol")
            {
                blocks.AddRange(ListItems(node));
            }
            else if (tag == "li")
            {
                blocks.Add(ListItem(node, "bullet"));
            }
            else if (tag == "blockquote")
            {
                blocks.AddRange(Quote(node));
            }
            else if (tag == "pre")
            {
                blocks.Add(CodeBlock(node));
            }
            else if (tag == "hr")
            {
                blocks.Add(new BitBlock { Type = BitBlockTypes.Divider.Name });
            }
            else if (tag == "table")
            {
                blocks.AddRange(TableRows(node));
            }
            else if (MediaTags.Contains(tag))
            {
                if (MediaSource(node) is { } link) blocks.Add(link);
            }
            else
            {
                // A container with no block of its own (a div, a stray table part): its content, block by block.
                blocks.AddRange(ReadBlocks(node.Children));
            }
        }

        Flush();
        return blocks;
    }

    // The blocks of inline content, which an image splits: the text before it, the image, the text after it.
    private static List<BitBlock> Paragraphs(IEnumerable<BitBlockHtmlTree> nodes, string type)
    {
        var blocks = new List<BitBlock>();
        var run = new List<BitBlockHtmlTree>();
        var images = 0;

        void Flush(bool always)
        {
            var text = InlineText(run);
            if (text.Length > 0 || always) blocks.Add(new BitBlock { Type = type, Text = text });
            run.Clear();
        }

        foreach (var node in nodes)
        {
            if (node.Tag == "img")
            {
                Flush(always: false);
                blocks.Add(Image(node));
                images++;
                continue;
            }

            run.Add(node);
        }

        // A paragraph with no image is kept even when empty: it is an empty line the author left.
        Flush(always: images == 0);
        return blocks;
    }

    private static string InlineText(IEnumerable<BitBlockHtmlTree> nodes)
        => BitBlockInline.Write(BitBlockInline.FromHtmlNodes(nodes));

    private static BitBlock Image(BitBlockHtmlTree node)
        => new()
        {
            Type = BitBlockTypes.Image.Name,
            Props = Props(
                ("src", node.GetAttribute("src")),
                ("alt", node.GetAttribute("alt")),
                ("width", int.TryParse(node.GetAttribute("width"), NumberStyles.None, CultureInfo.InvariantCulture, out var width)
                    ? width.ToString(CultureInfo.InvariantCulture)
                    : null),
                ("caption", node.GetAttribute("title")))
        };

    private static IEnumerable<BitBlock> ListItems(BitBlockHtmlTree list)
    {
        var ordered = list.Tag == "ol";
        foreach (var child in list.Children)
        {
            if (child.Tag == "li")
            {
                yield return ListItem(child, ordered ? "ordered" : "bullet");
            }
            else if (child.IsText is false || string.IsNullOrWhiteSpace(child.Text) is false)
            {
                // Content a list holds outside any item is read as an item of its own.
                yield return ListItem(child, ordered ? "ordered" : "bullet");
            }
        }
    }

    private static BitBlock ListItem(BitBlockHtmlTree item, string style)
    {
        var isTask = item.Tag == "li" && item.GetAttribute("data-checked") is not null;
        var nodes = item.Tag == "li" ? item.Children : [item];

        // The item's text is the inline content it opens with, or its first paragraph; everything after is nested.
        var text = new List<BitBlockHtmlTree>();
        var rest = new List<BitBlockHtmlTree>();
        var opened = false;
        foreach (var node in nodes)
        {
            if (opened is false && (node.IsText || InlineTags.Contains(node.Tag!)))
            {
                text.Add(node);
                continue;
            }

            if (opened is false && node.Tag == "p" && text.All(n => n.IsText && string.IsNullOrWhiteSpace(n.Text)))
            {
                text = [.. node.Children];
                opened = true;
                continue;
            }

            opened = true;
            rest.Add(node);
        }

        var children = ReadBlocks(rest);

        return new BitBlock
        {
            Type = BitBlockTypes.ListItem.Name,
            Text = InlineText(text),
            Props = Props(
                ("style", isTask ? "task" : style == "bullet" ? null : style),
                ("checked", isTask && item.GetAttribute("data-checked") == "true" ? "true" : null)),
            Children = children
        };
    }

    private static IEnumerable<BitBlock> Quote(BitBlockHtmlTree quote)
    {
        var blocks = ReadBlocks(quote.Children);
        if (blocks.Count == 0)
        {
            yield return new BitBlock { Type = BitBlockTypes.Quote.Name };
            yield break;
        }

        // Every paragraph of the quote is a quote block; anything else in it is read as itself.
        foreach (var block in blocks)
        {
            yield return block.Type == BitBlockTypes.Paragraph.Name ? block with { Type = BitBlockTypes.Quote.Name } : block;
        }
    }

    private static BitBlock CodeBlock(BitBlockHtmlTree pre)
    {
        var code = pre.Children.FirstOrDefault(c => c.Tag == "code");
        var language = code?.GetAttribute("class")?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(c => c.StartsWith("language-", StringComparison.Ordinal))?["language-".Length..];

        var text = pre.InnerText();
        // The newline that ends the last line is how the markup is written, not a line of the code.
        if (text.EndsWith('\n')) text = text[..^1];

        return new BitBlock
        {
            Type = BitBlockTypes.Code.Name,
            Text = text,
            Props = Props(("language", Language(language)))
        };
    }

    // A table has no block, so each of its rows is a paragraph of its cells' text.
    private static IEnumerable<BitBlock> TableRows(BitBlockHtmlTree table)
    {
        foreach (var row in Descendants(table).Where(n => n.Tag == "tr"))
        {
            var cells = row.Children.Where(c => c.Tag is "td" or "th").Select(c => BitBlockInline.Encode(c.InnerText().Trim()));
            var text = BitBlockInline.Normalize(string.Join(" | ", cells));
            if (text.Length > 0) yield return new BitBlock { Type = BitBlockTypes.Paragraph.Name, Text = text };
        }

        foreach (var caption in table.Children.Where(c => c.Tag == "caption"))
        {
            var text = InlineText(caption.Children);
            if (text.Length > 0) yield return new BitBlock { Type = BitBlockTypes.Paragraph.Name, Text = text };
        }
    }

    // Audio, video and embeds have no block, so each is kept as a link to what it plays.
    private static BitBlock? MediaSource(BitBlockHtmlTree media)
    {
        var src = media.GetAttribute("src") ?? Descendants(media).FirstOrDefault(n => n.Tag == "source")?.GetAttribute("src");
        if (string.IsNullOrWhiteSpace(src)) return null;

        var encoded = BitBlockInline.EncodeAttribute(src);
        var text = BitBlockInline.Normalize($"<a href=\"{encoded}\">{BitBlockInline.Encode(src)}</a>");
        return text.Length == 0 ? null : new BitBlock { Type = BitBlockTypes.Paragraph.Name, Text = text };
    }

    private static IEnumerable<BitBlockHtmlTree> Descendants(BitBlockHtmlTree node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    internal static IReadOnlyDictionary<string, string> Props(params (string Name, string? Value)[] props)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in props)
        {
            if (string.IsNullOrEmpty(value) is false) result[name] = value;
        }

        return result;
    }
}
