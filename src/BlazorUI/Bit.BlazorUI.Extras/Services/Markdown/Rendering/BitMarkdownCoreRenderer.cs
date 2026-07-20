using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders all basic CommonMark node types.</summary>
public sealed class BitMarkdownCoreRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownHeadingNode or BitMarkdownParagraphNode or BitMarkdownCodeBlockNode or BitMarkdownBlockquoteNode or BitMarkdownListNode
        or BitMarkdownThematicBreakNode or BitMarkdownTextNode or BitMarkdownEmphasisNode or BitMarkdownStrongNode or BitMarkdownCodeSpanNode
        or BitMarkdownLinkNode or BitMarkdownImageNode or BitMarkdownLineBreakNode;

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
                b.OpenElement(3, "pre");
                b.OpenElement(4, "code");
                if (!string.IsNullOrWhiteSpace(code.Info))
                    b.AddAttribute(5, "class", "language-"
                        + code.Info.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
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
        }
    }

    private static void WriteList(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownListNode list)
    {
        b.OpenElement(25, list.Ordered ? "ol" : "ul");
        if (list.Ordered && list.Start != 1)
            b.AddAttribute(26, "start", list.Start);

        foreach (var item in list.Items)
        {
            // The same literal is reused for every <li>; Blazor treats this like a
            // loop-rendered region and diffs the items by position.
            b.OpenElement(27, "li");
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
