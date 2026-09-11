using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders a <see cref="BitMarkdownFigureNode"/> as a figure with its caption.</summary>
public sealed class BitMarkdownFigureRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownFigureNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var figure = (BitMarkdownFigureNode)node;

        b.OpenElement(0, "figure");
        // The image itself is written by whichever renderer owns it, so the figure inherits every
        // safeguard the core renderer puts on an image (lazy loading, no referrer, ...).
        r.WriteNodes(b, figure.Children);
        b.OpenElement(1, "figcaption");
        b.AddContent(2, figure.Caption);
        b.CloseElement();
        b.CloseElement();
    }
}
