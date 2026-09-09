using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders a <see cref="BitMarkdownAlertNode"/> with the class names GitHub uses, so the
/// stylesheet can colour each kind without the renderer knowing about the palette.
/// </summary>
public sealed class BitMarkdownAlertRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownAlertNode;

    // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        var alert = (BitMarkdownAlertNode)node;
        string kind = alert.Kind.ToString().ToLowerInvariant();

        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", $"markdown-alert markdown-alert-{kind}");
        // The kind is announced rather than left to colour alone, which a screen reader
        // and a monochrome print both miss.
        b.OpenElement(2, "p");
        b.AddAttribute(3, "class", "markdown-alert-title");
        b.AddContent(4, alert.Kind.ToString());
        b.CloseElement();
        r.WriteNodes(b, alert.Children);
        b.CloseElement();
    }
}
