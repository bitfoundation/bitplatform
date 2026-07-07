using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownTaskCheckboxNode"/> as a disabled checkbox.</summary>
public sealed class BitMarkdownTaskCheckboxRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownTaskCheckboxNode;

    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
        var task = (BitMarkdownTaskCheckboxNode)node;
        b.OpenElement(0, "input");
        b.AddAttribute(1, "type", "checkbox");
        b.AddAttribute(2, "class", "task-list-item-checkbox");
        b.AddAttribute(3, "disabled", true);
        if (task.Checked) b.AddAttribute(4, "checked", true);
        b.CloseElement();
    }
}
