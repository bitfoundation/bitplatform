using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders all basic CommonMark node types.</summary>
public sealed class BitMarkdownCoreRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownHeadingNode or BitMarkdownParagraphNode or BitMarkdownCodeBlockNode or BitMarkdownBlockquoteNode or BitMarkdownListNode
        or BitMarkdownThematicBreakNode or BitMarkdownTextNode or BitMarkdownEmphasisNode or BitMarkdownStrongNode or BitMarkdownCodeSpanNode
        or BitMarkdownLinkNode or BitMarkdownImageNode or BitMarkdownLineBreakNode
        or BitMarkdownLinkReferenceDefinitionNode or BitMarkdownLinkReferenceNode;

    // Render-tree sequence numbers must be compile-time literals tied to a fixed
    // call site (never values produced at runtime), so Blazor's diff can match nodes
    // across renders. Each call site below uses a stable literal; recursion and loops
    // intentionally reuse the same literals, which Blazor handles. The ranges are
    // partitioned per renderer (core uses 0-99) to keep sibling sequences distinct.
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownHeadingNode h:
                b.OpenElement(0, "h" + h.Level);
                if (!string.IsNullOrEmpty(h.Id))
                    b.AddAttribute(1, "id", h.Id);
                r.WriteNodes(b, h.Inlines);
                b.CloseElement();
                break;

            case BitMarkdownParagraphNode p:
                b.OpenElement(2, "p");
                r.WriteNodes(b, p.Inlines);
                b.CloseElement();
                break;

            case BitMarkdownCodeBlockNode code:
                // The language class goes on both elements: a highlighter reads it off the <code>,
                // while several of their plugins (line numbers, toolbars) read it off the <pre>.
                // The info string may carry more than the language, so only its first word is used.
                string? language = string.IsNullOrWhiteSpace(code.Info)
                    ? null
                    : "language-" + code.Info.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0];
                b.OpenElement(3, "pre");
                if (language is not null)
                    b.AddAttribute(35, "class", language);
                b.OpenElement(4, "code");
                if (language is not null)
                    b.AddAttribute(5, "class", language);
                b.AddContent(6, code.Content);
                b.CloseElement();
                b.CloseElement();
                break;

            case BitMarkdownBlockquoteNode bq:
                b.OpenElement(7, "blockquote");
                r.WriteNodes(b, bq.Children);
                b.CloseElement();
                break;

            case BitMarkdownListNode list:
                WriteList(r, b, list);
                break;

            case BitMarkdownThematicBreakNode:
                b.OpenElement(8, "hr");
                b.CloseElement();
                break;

            case BitMarkdownTextNode text:
                b.AddContent(9, text.Text);
                break;

            case BitMarkdownEmphasisNode em:
                b.OpenElement(10, "em");
                r.WriteNodes(b, em.Children);
                b.CloseElement();
                break;

            case BitMarkdownStrongNode strong:
                b.OpenElement(11, "strong");
                r.WriteNodes(b, strong.Children);
                b.CloseElement();
                break;

            case BitMarkdownCodeSpanNode cs:
                b.OpenElement(12, "code");
                b.AddContent(13, cs.Content);
                b.CloseElement();
                break;

            case BitMarkdownLinkNode link:
                b.OpenElement(14, "a");
                if (!string.IsNullOrEmpty(link.Url))
                {
                    b.AddAttribute(15, "href", link.Url);
                    if (IsExternal(link.Url))
                    {
                        b.AddAttribute(16, "target", "_blank");
                        b.AddAttribute(17, "rel", "noopener noreferrer");
                    }
                }
                if (!string.IsNullOrEmpty(link.Title))
                    b.AddAttribute(18, "title", link.Title);
                r.WriteNodes(b, link.Children);
                b.CloseElement();
                break;

            case BitMarkdownImageNode img:
                b.OpenElement(19, "img");
                if (!string.IsNullOrEmpty(img.Url))
                    b.AddAttribute(20, "src", img.Url);
                b.AddAttribute(21, "alt", img.Alt);
                if (!string.IsNullOrEmpty(img.Title))
                    b.AddAttribute(22, "title", img.Title);
                // Never leak the (possibly token-bearing) page URL to the image host.
                b.AddAttribute(28, "referrerpolicy", "no-referrer");
                // A long document is mostly off-screen when it first paints, so images
                // fetch and decode only as they approach the viewport.
                b.AddAttribute(29, "loading", "lazy");
                b.AddAttribute(30, "decoding", "async");
                b.CloseElement();
                break;

            case BitMarkdownLineBreakNode lb:
                if (lb.Hard)
                {
                    b.OpenElement(23, "br");
                    b.CloseElement();
                }
                else
                {
                    b.AddContent(24, "\n");
                }
                break;

            case BitMarkdownLinkReferenceDefinitionNode:
                // A definition declares a label; it is not part of the rendered document.
                // BitMarkdownLinkReferenceAstProcessor normally removes it before this
                // point, so reaching here means the AST was rendered without the core
                // processors - render nothing rather than the raw definition line.
                break;

            case BitMarkdownLinkReferenceNode reference:
                // An unresolved reference reads as the text it was written as.
                b.AddContent(31, reference.RawPrefix);
                r.WriteNodes(b, reference.Children);
                b.AddContent(32, reference.RawSuffix);
                break;
        }
    }

    private static void WriteList(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownListNode list)
    {
        b.OpenElement(25, list.Ordered ? "ol" : "ul");
        if (list.Ordered && list.Start != 1)
            b.AddAttribute(26, "start", list.Start);
        // The class GitHub puts on a list holding checkboxes, so a stylesheet can drop the
        // bullets without depending on ":has()".
        if (list.Items.Exists(i => i.IsTask))
            b.AddAttribute(33, "class", "contains-task-list");

        foreach (var item in list.Items)
        {
            // The same literal is reused for every <li>; Blazor treats this like a
            // loop-rendered region and diffs the items by position.
            b.OpenElement(27, "li");
            if (item.IsTask)
                b.AddAttribute(34, "class", "task-list-item");
            // Tight lists render a lone paragraph's inlines directly inside <li>.
            if (list.Tight)
            {
                foreach (var child in item.Children)
                {
                    if (child is BitMarkdownParagraphNode para)
                        r.WriteNodes(b, para.Inlines);
                    else
                        r.WriteNode(b, child);
                }
            }
            else
            {
                r.WriteNodes(b, item.Children);
            }
            b.CloseElement();
        }
        b.CloseElement();
    }

    private static bool IsExternal(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
