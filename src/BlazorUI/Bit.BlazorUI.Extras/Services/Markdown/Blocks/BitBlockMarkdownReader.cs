using System.Globalization;
using Node = Bit.BlazorUI.BitBlockInline.Node;
using TextNode = Bit.BlazorUI.BitBlockInline.TextNode;
using ElementNode = Bit.BlazorUI.BitBlockInline.ElementNode;

namespace Bit.BlazorUI;

/// <summary>
/// Reads Markdown into blocks, through <see cref="BitMarkdownParser"/> and a mapping of its tree.
/// </summary>
/// <remarks>
/// The pipeline is CommonMark with strikethrough, task lists and the emphasis extensions - exactly the syntax
/// <see cref="BitBlockMarkdownWriter"/> writes. Bare addresses are not turned into links: a writer has no way to keep
/// one from becoming a link, so a round trip would add links the document never had.
/// </remarks>
internal static class BitBlockMarkdownReader
{
    private static readonly Lazy<BitMarkdownPipeline> Pipeline = new(() => new BitMarkdownPipelineBuilder()
        .UseStrikethrough()
        .UseTaskLists()
        .UseEmphasisExtras()
        .Build());

    public static IReadOnlyList<BitBlock> Read(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return [];

        var document = BitMarkdownParser.Parse(markdown, Pipeline.Value);
        return ReadBlocks(document.Children);
    }

    private static List<BitBlock> ReadBlocks(IEnumerable<BitMarkdownNode> nodes)
    {
        var blocks = new List<BitBlock>();
        foreach (var node in nodes)
        {
            switch (node)
            {
                case BitMarkdownParagraphNode paragraph:
                    blocks.AddRange(Paragraphs(paragraph.Inlines, BitBlockTypes.Paragraph.Name));
                    break;

                case BitMarkdownHeadingNode heading:
                    blocks.Add(new BitBlock
                    {
                        Type = BitBlockTypes.Heading.Name,
                        Text = InlineText(heading.Inlines),
                        Props = BitBlockHtmlConverter.Props(("level", heading.Level.ToString(CultureInfo.InvariantCulture)))
                    });
                    break;

                case BitMarkdownThematicBreakNode:
                    blocks.Add(new BitBlock { Type = BitBlockTypes.Divider.Name });
                    break;

                case BitMarkdownCodeBlockNode code:
                    blocks.Add(CodeBlock(code));
                    break;

                case BitMarkdownBlockquoteNode quote:
                    blocks.AddRange(Quote(quote));
                    break;

                case BitMarkdownListNode list:
                    foreach (var item in list.Items) blocks.Add(ListItem(item, list.Ordered));
                    break;

                case BitMarkdownLinkReferenceDefinitionNode:
                    // Already resolved into the links that use it.
                    break;

                default:
                    if (node.ChildNodes is { Count: > 0 } children && children.All(c => IsBlock(c)))
                    {
                        blocks.AddRange(ReadBlocks(children));
                    }
                    else
                    {
                        // A block with no block of its own: a paragraph of its text, so nothing is lost silently.
                        var text = BitBlockInline.Normalize(BitBlockInline.Encode(BitMarkdownInlineHelpers.PlainText(node.ChildNodes ?? [])));
                        if (text.Length > 0) blocks.Add(new BitBlock { Type = BitBlockTypes.Paragraph.Name, Text = text });
                    }
                    break;
            }
        }

        return blocks;
    }

    private static bool IsBlock(BitMarkdownNode node)
        => node is BitMarkdownParagraphNode or BitMarkdownHeadingNode or BitMarkdownThematicBreakNode or BitMarkdownCodeBlockNode
            or BitMarkdownBlockquoteNode or BitMarkdownListNode;

    // A paragraph's blocks, which an image splits: the text before it, the image, the text after it. An image alone is
    // an image block.
    private static List<BitBlock> Paragraphs(IEnumerable<BitMarkdownNode> inlines, string type)
    {
        var blocks = new List<BitBlock>();
        var run = new List<BitMarkdownNode>();

        void Flush()
        {
            var text = InlineText(run);
            if (text.Length > 0) blocks.Add(new BitBlock { Type = type, Text = text });
            run.Clear();
        }

        foreach (var inline in inlines)
        {
            if (inline is BitMarkdownImageNode image)
            {
                Flush();
                blocks.Add(new BitBlock
                {
                    Type = BitBlockTypes.Image.Name,
                    Props = BitBlockHtmlConverter.Props(("src", image.Url), ("alt", image.Alt), ("caption", image.Title))
                });
                continue;
            }

            run.Add(inline);
        }

        Flush();
        return blocks;
    }

    private static BitBlock CodeBlock(BitMarkdownCodeBlockNode code)
    {
        var text = code.Content.Replace("\r\n", "\n");
        // The newline that ends the last line is how the fence is written, not a line of the code.
        if (text.EndsWith('\n')) text = text[..^1];

        var language = code.Info?.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        return new BitBlock
        {
            Type = BitBlockTypes.Code.Name,
            Text = text,
            Props = BitBlockHtmlConverter.Props(("language", BitBlockHtmlConverter.Language(language)))
        };
    }

    private static IEnumerable<BitBlock> Quote(BitMarkdownBlockquoteNode quote)
    {
        if (quote.Children.Count == 0)
        {
            yield return new BitBlock { Type = BitBlockTypes.Quote.Name };
            yield break;
        }

        // Every paragraph of the quote is a quote block; anything else in it is read as itself.
        foreach (var child in quote.Children)
        {
            if (child is BitMarkdownParagraphNode paragraph)
            {
                foreach (var block in Paragraphs(paragraph.Inlines, BitBlockTypes.Quote.Name)) yield return block;
            }
            else
            {
                foreach (var block in ReadBlocks([child])) yield return block;
            }
        }
    }

    private static BitBlock ListItem(BitMarkdownListItemNode item, bool ordered)
    {
        var checkbox = item.Children.FirstOrDefault() is BitMarkdownParagraphNode first
            ? first.Inlines.OfType<BitMarkdownTaskCheckboxNode>().FirstOrDefault()
            : null;

        var style = checkbox is not null ? "task" : ordered ? "ordered" : null;

        // The item's text is its first paragraph; everything after it is nested. An image in that paragraph splits
        // it, and what follows the text is nested too.
        var text = "";
        var children = new List<BitBlock>();
        var rest = item.Children.AsEnumerable();

        if (item.Children.FirstOrDefault() is BitMarkdownParagraphNode paragraph)
        {
            var opening = Paragraphs(paragraph.Inlines, BitBlockTypes.Paragraph.Name);
            if (opening.Count > 0 && opening[0].Type == BitBlockTypes.Paragraph.Name)
            {
                text = opening[0].Text;
                opening.RemoveAt(0);
            }
            children.AddRange(opening);
            rest = item.Children.Skip(1);
        }

        children.AddRange(ReadBlocks(rest));

        return new BitBlock
        {
            Type = BitBlockTypes.ListItem.Name,
            Text = text,
            Props = BitBlockHtmlConverter.Props(("style", style), ("checked", checkbox?.Checked is true ? "true" : null)),
            Children = children
        };
    }

    private static string InlineText(IEnumerable<BitMarkdownNode> inlines)
        => BitBlockInline.Normalize(BitBlockInline.Write(BitBlockInline.Canonical(Inlines(inlines))));

    private static List<Node> Inlines(IEnumerable<BitMarkdownNode> inlines)
    {
        var result = new List<Node>();
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case BitMarkdownTextNode text:
                    result.Add(new TextNode(text.Text));
                    break;
                case BitMarkdownStrongNode strong:
                    result.Add(new ElementNode("b", null, Inlines(strong.Children)));
                    break;
                case BitMarkdownEmphasisNode emphasis:
                    result.Add(new ElementNode("i", null, Inlines(emphasis.Children)));
                    break;
                case BitMarkdownStrikethroughNode strike:
                    result.Add(new ElementNode("s", null, Inlines(strike.Children)));
                    break;
                case BitMarkdownSubscriptNode sub:
                    result.Add(new ElementNode("sub", null, Inlines(sub.Children)));
                    break;
                case BitMarkdownSuperscriptNode sup:
                    result.Add(new ElementNode("sup", null, Inlines(sup.Children)));
                    break;
                case BitMarkdownMarkedNode marked:
                    result.Add(new ElementNode("mark", null, Inlines(marked.Children)));
                    break;
                case BitMarkdownInsertedNode inserted:
                    result.Add(new ElementNode("ins", null, Inlines(inserted.Children)));
                    break;
                case BitMarkdownCodeSpanNode code:
                    result.Add(new ElementNode("code", null, [new TextNode(code.Content)]));
                    break;
                case BitMarkdownLinkNode link:
                    result.Add(new ElementNode("a", link.Url, Inlines(link.Children)));
                    break;
                case BitMarkdownLinkReferenceNode reference:
                    // A reference with no definition stays the text it was written as.
                    result.Add(new TextNode(reference.RawPrefix));
                    result.AddRange(Inlines(reference.Children));
                    result.Add(new TextNode(reference.RawSuffix));
                    break;
                case BitMarkdownLineBreakNode lineBreak:
                    result.Add(lineBreak.Hard ? new ElementNode("br", null, []) : new TextNode(" "));
                    break;
                case BitMarkdownImageNode image:
                    // An image inside a link or a mark has no block to be: its description stands in for it.
                    result.Add(new TextNode(image.Alt));
                    break;
                case BitMarkdownTaskCheckboxNode:
                    // Read as the item's task state.
                    break;
                default:
                    if (inline.ChildNodes is { } children) result.AddRange(Inlines(children));
                    break;
            }
        }

        return result;
    }
}
