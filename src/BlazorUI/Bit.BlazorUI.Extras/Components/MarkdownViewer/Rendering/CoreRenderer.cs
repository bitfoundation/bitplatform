using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders all basic CommonMark node types.</summary>
public sealed class CoreRenderer : NodeRenderer
{
    public override bool Accept(MarkdownNode node) => node is
        HeadingNode or ParagraphNode or CodeBlockNode or BlockquoteNode or ListNode
        or ThematicBreakNode or TextNode or EmphasisNode or StrongNode or CodeSpanNode
        or LinkNode or ImageNode or LineBreakNode;

    public override void Write(MarkdownRenderer r, RenderTreeBuilder b, MarkdownNode node)
    {
        switch (node)
        {
            case HeadingNode h:
                b.OpenElement(r.NextSeq(), "h" + h.Level);
                if (!string.IsNullOrEmpty(h.Id))
                    b.AddAttribute(r.NextSeq(), "id", h.Id);
                r.WriteNodes(b, h.Inlines);
                b.CloseElement();
                break;

            case ParagraphNode p:
                b.OpenElement(r.NextSeq(), "p");
                r.WriteNodes(b, p.Inlines);
                b.CloseElement();
                break;

            case CodeBlockNode code:
                b.OpenElement(r.NextSeq(), "pre");
                b.OpenElement(r.NextSeq(), "code");
                if (!string.IsNullOrEmpty(code.Info))
                    b.AddAttribute(r.NextSeq(), "class", "language-" + code.Info.Split(' ', 2)[0]);
                b.AddContent(r.NextSeq(), code.Content);
                b.CloseElement();
                b.CloseElement();
                break;

            case BlockquoteNode bq:
                b.OpenElement(r.NextSeq(), "blockquote");
                r.WriteNodes(b, bq.Children);
                b.CloseElement();
                break;

            case ListNode list:
                WriteList(r, b, list);
                break;

            case ThematicBreakNode:
                b.OpenElement(r.NextSeq(), "hr");
                b.CloseElement();
                break;

            case TextNode text:
                b.AddContent(r.NextSeq(), text.Text);
                break;

            case EmphasisNode em:
                b.OpenElement(r.NextSeq(), "em");
                r.WriteNodes(b, em.Children);
                b.CloseElement();
                break;

            case StrongNode strong:
                b.OpenElement(r.NextSeq(), "strong");
                r.WriteNodes(b, strong.Children);
                b.CloseElement();
                break;

            case CodeSpanNode cs:
                b.OpenElement(r.NextSeq(), "code");
                b.AddContent(r.NextSeq(), cs.Content);
                b.CloseElement();
                break;

            case LinkNode link:
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

            case ImageNode img:
                b.OpenElement(r.NextSeq(), "img");
                if (!string.IsNullOrEmpty(img.Url))
                    b.AddAttribute(r.NextSeq(), "src", img.Url);
                b.AddAttribute(r.NextSeq(), "alt", img.Alt);
                if (!string.IsNullOrEmpty(img.Title))
                    b.AddAttribute(r.NextSeq(), "title", img.Title);
                b.CloseElement();
                break;

            case LineBreakNode lb:
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

    private static void WriteList(MarkdownRenderer r, RenderTreeBuilder b, ListNode list)
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
                    if (child is ParagraphNode para)
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
