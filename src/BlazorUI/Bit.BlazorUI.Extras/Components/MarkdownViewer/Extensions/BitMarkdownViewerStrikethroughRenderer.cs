using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownViewerStrikethroughNode"/>.</summary>
public sealed class BitMarkdownViewerStrikethroughRenderer : BitMarkdownViewerNodeRenderer
{
    public override bool Accept(BitMarkdownViewerMarkdownNode node) => node is BitMarkdownViewerStrikethroughNode;

    public override void Write(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerMarkdownNode node)
    {
        // Fixed literal sequence number (see BitMarkdownViewerCoreRenderer for the rationale).
        b.OpenElement(0, "del");
        r.WriteNodes(b, ((BitMarkdownViewerStrikethroughNode)node).Children);
        b.CloseElement();
    }
}
