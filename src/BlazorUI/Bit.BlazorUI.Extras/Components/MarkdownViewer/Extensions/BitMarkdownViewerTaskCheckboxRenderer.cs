using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>Renders <see cref="BitMarkdownViewerTaskCheckboxNode"/> as a disabled checkbox.</summary>
public sealed class BitMarkdownViewerTaskCheckboxRenderer : BitMarkdownViewerNodeRenderer
{
    public override bool Accept(BitMarkdownViewerMarkdownNode node) => node is BitMarkdownViewerTaskCheckboxNode;

    public override void Write(BitMarkdownViewerMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownViewerMarkdownNode node)
    {
        var task = (BitMarkdownViewerTaskCheckboxNode)node;
        b.OpenElement(r.NextSeq(), "input");
        b.AddAttribute(r.NextSeq(), "type", "checkbox");
        b.AddAttribute(r.NextSeq(), "class", "task-list-item-checkbox");
        b.AddAttribute(r.NextSeq(), "disabled", true);
        if (task.Checked) b.AddAttribute(r.NextSeq(), "checked", true);
        b.CloseElement();
    }
}
