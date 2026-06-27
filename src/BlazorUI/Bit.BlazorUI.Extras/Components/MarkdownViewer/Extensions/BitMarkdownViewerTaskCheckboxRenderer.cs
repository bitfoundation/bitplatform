using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownViewerTaskCheckboxNode"/> as a disabled checkbox.</summary>
public sealed class BitMarkdownViewerTaskCheckboxRenderer : BitMarkdownViewerNodeRenderer
{
    public override bool Accept(BitMarkdownViewerMarkdownNode node) => node is BitMarkdownViewerTaskCheckboxNode;

    public override void Write(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerMarkdownNode node)
    {
        // Fixed literal sequence numbers (see BitMarkdownViewerCoreRenderer for the rationale).
        var task = (BitMarkdownViewerTaskCheckboxNode)node;
        b.OpenElement(0, "input");
        b.AddAttribute(1, "type", "checkbox");
        b.AddAttribute(2, "class", "task-list-item-checkbox");
        b.AddAttribute(3, "disabled", true);
        if (task.Checked) b.AddAttribute(4, "checked", true);
        b.CloseElement();
    }
}
