using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders all basic CommonMark node types.</summary>
public sealed class BitMarkdownViewerCoreRenderer : BitMarkdownViewerNodeRenderer
{
    public override bool Accept(BitMarkdownViewerMarkdownNode node) => node is
        BitMarkdownViewerHeadingNode or BitMarkdownViewerParagraphNode or BitMarkdownViewerCodeBlockNode or BitMarkdownViewerBlockquoteNode or BitMarkdownViewerListNode
        or BitMarkdownViewerThematicBreakNode or BitMarkdownViewerTextNode or BitMarkdownViewerEmphasisNode or BitMarkdownViewerStrongNode or BitMarkdownViewerCodeSpanNode
        or BitMarkdownViewerLinkNode or BitMarkdownViewerImageNode or BitMarkdownViewerLineBreakNode;

    public override void Write(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownViewerHeadingNode h:
                b.OpenElement(r.NextSeq(), "h" + h.Level);
                if (!string.IsNullOrEmpty(h.Id))
                    b.AddAttribute(r.NextSeq(), "id", h.Id);
                r.WriteNodes(b, h.Inlines);
                b.CloseElement();
                break;

            case BitMarkdownViewerParagraphNode p:
                b.OpenElement(r.NextSeq(), "p");
                r.WriteNodes(b, p.Inlines);
                b.CloseElement();
                break;

            case BitMarkdownViewerCodeBlockNode code:
                b.OpenElement(r.NextSeq(), "pre");
                b.OpenElement(r.NextSeq(), "code");
                if (!string.IsNullOrEmpty(code.Info))
                    b.AddAttribute(r.NextSeq(), "class", "language-"
                        + code.Info.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
                b.AddContent(r.NextSeq(), code.Content);
                b.CloseElement();
                b.CloseElement();
                break;

            case BitMarkdownViewerBlockquoteNode bq:
                b.OpenElement(r.NextSeq(), "blockquote");
                r.WriteNodes(b, bq.Children);
                b.CloseElement();
                break;

            case BitMarkdownViewerListNode list:
                WriteList(r, b, list);
                break;

            case BitMarkdownViewerThematicBreakNode:
                b.OpenElement(r.NextSeq(), "hr");
                b.CloseElement();
                break;

            case BitMarkdownViewerTextNode text:
                b.AddContent(r.NextSeq(), text.Text);
                break;

            case BitMarkdownViewerEmphasisNode em:
                b.OpenElement(r.NextSeq(), "em");
                r.WriteNodes(b, em.Children);
                b.CloseElement();
                break;

            case BitMarkdownViewerStrongNode strong:
                b.OpenElement(r.NextSeq(), "strong");
                r.WriteNodes(b, strong.Children);
                b.CloseElement();
                break;

            case BitMarkdownViewerCodeSpanNode cs:
                b.OpenElement(r.NextSeq(), "code");
                b.AddContent(r.NextSeq(), cs.Content);
                b.CloseElement();
                break;

            case BitMarkdownViewerLinkNode link:
                b.OpenElement(r.NextSeq(), "a");
                if (!string.IsNullOrEmpty(link.Url))
                {
                    b.AddAttribute(r.NextSeq(), "href", link.Url);
                    if (IsExternal(link.Url))
                    {
                        b.AddAttribute(r.NextSeq(), "target", "_blank");
                        b.AddAttribute(r.NextSeq(), "rel", "noopener noreferrer");
                    }
                }
                if (!string.IsNullOrEmpty(link.Title))
                    b.AddAttribute(r.NextSeq(), "title", link.Title);
                r.WriteNodes(b, link.Children);
                b.CloseElement();
                break;

            case BitMarkdownViewerImageNode img:
                b.OpenElement(r.NextSeq(), "img");
                if (!string.IsNullOrEmpty(img.Url))
                    b.AddAttribute(r.NextSeq(), "src", img.Url);
                b.AddAttribute(r.NextSeq(), "alt", img.Alt);
                if (!string.IsNullOrEmpty(img.Title))
                    b.AddAttribute(r.NextSeq(), "title", img.Title);
                b.CloseElement();
                break;

            case BitMarkdownViewerLineBreakNode lb:
                if (lb.Hard)
                {
                    b.OpenElement(r.NextSeq(), "br");
                    b.CloseElement();
                }
                else
                {
                    b.AddContent(r.NextSeq(), "\n");
                }
                break;
        }
    }

    private static void WriteList(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerListNode list)
    {
        b.OpenElement(r.NextSeq(), list.Ordered ? "ol" : "ul");
        if (list.Ordered && list.Start != 1)
            b.AddAttribute(r.NextSeq(), "start", list.Start);

        foreach (var item in list.Items)
        {
            b.OpenElement(r.NextSeq(), "li");
            // Tight lists render a lone paragraph's inlines directly inside <li>.
            if (list.Tight)
            {
                foreach (var child in item.Children)
                {
                    if (child is BitMarkdownViewerParagraphNode para)
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
