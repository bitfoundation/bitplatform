using System.Globalization;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// Renders <see cref="BitMarkdownTaskCheckboxNode"/> as a checkbox - disabled, the way GitHub
/// renders one, unless the host is listening for changes to it.
/// </summary>
public sealed class BitMarkdownTaskCheckboxRenderer : BitMarkdownNodeRenderer
{
    public override bool Accept(BitMarkdownNode node) => node is BitMarkdownTaskCheckboxNode;

    public override void Write(BitMarkdownRenderer r, RenderTreeBuilder b, BitMarkdownNode node)
    {
        // Fixed literal sequence numbers (see BitMarkdownCoreRenderer for the rationale).
        var task = (BitMarkdownTaskCheckboxNode)node;
        bool interactive = task.OnChange is { HasDelegate: true };

        b.OpenElement(0, "input");
        b.AddAttribute(1, "type", "checkbox");
        b.AddAttribute(2, "class", "task-list-item-checkbox");
        if (interactive is false) b.AddAttribute(3, "disabled", true);
        if (task.Checked) b.AddAttribute(4, "checked", true);
        if (interactive)
        {
            // A checkbox with no visible label of its own is named after the item it opens, which
            // is the text right beside it.
            b.AddAttribute(5, "aria-label", string.Format(CultureInfo.CurrentCulture, r.Texts.Task, task.Index + 1));
            b.AddAttribute(6, "onchange", task.OnChange!.Value);
        }
        b.CloseElement();
    }
}
