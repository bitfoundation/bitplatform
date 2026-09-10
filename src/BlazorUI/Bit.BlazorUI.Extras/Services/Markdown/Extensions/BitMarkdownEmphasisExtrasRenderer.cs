using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders the subscript, superscript, inserted and highlighted inline nodes.</summary>
public sealed class BitMarkdownEmphasisExtrasRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is
        BitMarkdownSubscriptNode or BitMarkdownSuperscriptNode or BitMarkdownInsertedNode or BitMarkdownMarkedNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        switch (node)
        {
            case BitMarkdownSubscriptNode sub:
                b.OpenElement(0, "sub");
                r.WriteNodes(b, sub.Children);
                b.CloseElement();
                break;

            case BitMarkdownSuperscriptNode sup:
                b.OpenElement(1, "sup");
                r.WriteNodes(b, sup.Children);
                b.CloseElement();
                break;

            case BitMarkdownInsertedNode ins:
                b.OpenElement(2, "ins");
                r.WriteNodes(b, ins.Children);
                b.CloseElement();
                break;

            case BitMarkdownMarkedNode mark:
                b.OpenElement(3, "mark");
                r.WriteNodes(b, mark.Children);
                b.CloseElement();
                break;
        }
    }
}
