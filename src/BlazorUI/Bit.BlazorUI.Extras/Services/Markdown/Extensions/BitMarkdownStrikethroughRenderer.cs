using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownStrikethroughNode"/>.</summary>
public sealed class BitMarkdownStrikethroughRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownStrikethroughNode;

    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        // Fixed literal sequence number (see BitMarkdownCoreRenderer for the rationale).
        b.OpenElement(0, "del");
        r.WriteNodes(b, ((BitMarkdownStrikethroughNode)node).Children);
        b.CloseElement();
    }
}
