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
        // A read-only box is disabled the way every disabled native control of the library is: with
        // aria-disabled beside the disabled attribute, so either selector finds it.
        if (interactive is false)
        {
            b.AddAttribute(3, "disabled", true);
            b.AddAttribute(4, "aria-disabled", "true");
        }
        if (task.Checked) b.AddAttribute(5, "checked", true);
        if (interactive)
        {
            // A checkbox with no label element of its own is named after the item it opens - the
            // text right beside it, so the name a reader hears is the one they see. Only an item
            // with no text falls back to its number.
            b.AddAttribute(6, "aria-label", string.IsNullOrWhiteSpace(task.Label)
                ? string.Format(CultureInfo.CurrentCulture, r.Texts.Task, task.Index + 1)
                : task.Label);
            b.AddAttribute(7, "onchange", task.OnChange!.Value);
        }
        b.CloseElement();
    }
}
